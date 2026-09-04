using System;
using System.Collections;
using System.Collections.Generic;
using Arborvale.Shared;
using UnityEngine;

/// <summary>
/// Placed in the world as a craftable station. The player opens the grafting UI,
/// slots two branches, and waits for the bench timer to complete.
/// Success rolls locally in offline/demo mode, or via the backend in online mode.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GraftingBench : MonoBehaviour, IInteractable
{
    [Header("Recipes")]
    public List<GraftRecipe> allRecipes = new List<GraftRecipe>();

    [Header("Highlight")]
    public MeshRenderer benchRenderer;
    public Material highlightMaterial;
    private Material originalMaterial;

    private bool isBusy = false;

    public string InteractionPrompt => isBusy ? "Grafting in progress..." : "Open Graft Bench";

    private void Start()
    {
        if (benchRenderer == null)
            benchRenderer = GetComponent<MeshRenderer>();
        if (benchRenderer != null && benchRenderer.material != null)
            originalMaterial = benchRenderer.material;
    }

    public void Highlight(bool active)
    {
        if (benchRenderer == null || highlightMaterial == null) return;
        benchRenderer.material = active ? highlightMaterial : originalMaterial;
    }

    public void Interact()
    {
        if (isBusy)
        {
            HUDManager.Instance?.ShowMessage("The graft bench is busy.");
            return;
        }
        GraftingUIManager.Instance?.ShowGraftingUI(this);
    }

    /// <summary>
    /// Called by GraftingUIManager when the player confirms a graft.
    /// Consumes inputs, runs the timer, then resolves.
    /// </summary>
    public void StartGraft(GraftRecipe recipe, int bloomIndex = 0)
    {
        if (isBusy) return;

        string branchA = $"Branch:{recipe.speciesA.seedName}";
        string branchB = $"Branch:{recipe.speciesB.seedName}";

        // Use server costs when online; fall back to recipe demo values when offline
        var serverCost = GraftConfigService.Instance?.GetCost(recipe.speciesA.seedName, recipe.speciesB.seedName);
        string fertilizer = serverCost?.FertilizerItemName ?? recipe.GetFertilizerItemName();
        int fertAmount = serverCost != null ? serverCost.FertilizerAmount : recipe.GetFertilizerAmount();
        float graftTime = serverCost != null ? serverCost.GraftTimeSeconds : recipe.graftTimeSeconds;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        if (!inv.HasItem(branchA, ItemType.Material, 1) ||
            !inv.HasItem(branchB, ItemType.Material, 1) ||
            (fertAmount > 0 && !inv.HasItem(fertilizer, ItemType.Material, fertAmount)))
        {
            HUDManager.Instance?.ShowMessage("Missing materials for graft.");
            return;
        }

        inv.RemoveItem(branchA, ItemType.Material, 1);
        inv.RemoveItem(branchB, ItemType.Material, 1);
        if (fertAmount > 0)
            inv.RemoveItem(fertilizer, ItemType.Material, fertAmount);

        isBusy = true;
        HUDManager.Instance?.ShowMessage($"Grafting... ({graftTime}s)");
        StartCoroutine(GraftRoutine(recipe, bloomIndex, graftTime));
    }

    private IEnumerator GraftRoutine(GraftRecipe recipe, int bloomIndex, float overrideTime = -1f)
    {
        float waitTime = overrideTime >= 0f ? overrideTime : recipe.graftTimeSeconds;
        yield return new WaitForSeconds(waitTime);

        isBusy = false;

        if (OnlineServices.IsAvailable)
        {
            // Online path: fire-and-forget async call — result handled via callback
            StartCoroutine(OnlineGraftRoutine(recipe, bloomIndex));
        }
        else
        {
            ResolveOffline(recipe, bloomIndex);
        }
    }

    private void ResolveOffline(GraftRecipe recipe, int bloomIndex)
    {
        float chance = GraftConfig.Instance != null ? GraftConfig.Instance.demoSuccessChance : 0.75f;
        bool success = UnityEngine.Random.value <= chance;

        if (success && recipe.resultSeedData != null)
        {
            InventoryManager.Instance?.AddItem(
                recipe.resultSeedData.seedName, ItemType.Seed, 1,
                recipe.resultSeedData.seedIcon);
            HUDManager.Instance?.ShowMessage($"Graft successful! You got a {recipe.resultSeedData.seedName} seed.");
        }
        else
        {
            HUDManager.Instance?.ShowMessage("Graft failed. The branches were incompatible.");
        }
    }

    private IEnumerator OnlineGraftRoutine(GraftRecipe recipe, int bloomIndex)
    {
        if (recipe.resultTrunkPart == null || recipe.resultFoliagePart == null ||
            bloomIndex >= recipe.allowedBloomParts.Count)
        {
            ResolveOffline(recipe, bloomIndex);
            yield break;
        }

        var bloomPart = recipe.allowedBloomParts[bloomIndex];
        string idempotencyKey = Guid.NewGuid().ToString();
        var task = OnlineServices.Instance.AttemptGraftAsync(
            recipe.resultTrunkPart.partId,
            recipe.resultFoliagePart.partId,
            bloomPart.partId,
            idempotencyKey);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            Debug.LogWarning("[GraftingBench] Backend graft failed, falling back to offline resolution.");
            ResolveOffline(recipe, bloomIndex);
            yield break;
        }

        var result = task.Result;
        if (result.Wallet != null)
            WalletWidget.Instance?.Refresh(result.Wallet);

        if (result.Success && recipe.resultSeedData != null)
        {
            InventoryManager.Instance?.AddItem(
                recipe.resultSeedData.seedName, ItemType.Seed, 1,
                recipe.resultSeedData.seedIcon);

            // Register the server grantId so this hybrid seed can be traded online
            if (!string.IsNullOrEmpty(result.GrantId))
                GrantStore.Instance?.AddGrant(recipe.resultSeedData.seedName, result.GrantId);

            HUDManager.Instance?.ShowMessage($"Graft successful! You got a {recipe.resultSeedData.seedName} seed.");
        }
        else
        {
            HUDManager.Instance?.ShowMessage("Graft failed.");
        }
    }

    public List<GraftRecipe> GetAvailableRecipes()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return new List<GraftRecipe>();

        var available = new List<GraftRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe == null || recipe.speciesA == null || recipe.speciesB == null) continue;
            string branchA = $"Branch:{recipe.speciesA.seedName}";
            string branchB = $"Branch:{recipe.speciesB.seedName}";
            if (inv.HasItem(branchA, ItemType.Material, 1) && inv.HasItem(branchB, ItemType.Material, 1))
                available.Add(recipe);
        }
        return available;
    }
}
