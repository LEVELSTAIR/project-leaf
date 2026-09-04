using UnityEngine;

/// <summary>
/// Placed as a child object on seedling prefabs (1-2 per plant).
/// Adds a small sphere collider (on the interactionLayer) that lets the player
/// break off a branch during the Early growth stage.
/// One break per plant: disables itself after use.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BranchBreakable : MonoBehaviour, IInteractable
{
    [Tooltip("The PlantPot that owns this branch. Auto-found in parent if left null.")]
    public PlantPot parentPot;

    private MeshRenderer highlightRenderer;
    private Material originalMaterial;

    [Header("Highlight")]
    public Material highlightMaterial;

    private void Start()
    {
        if (parentPot == null)
            parentPot = GetComponentInParent<PlantPot>();

        highlightRenderer = GetComponent<MeshRenderer>();
        if (highlightRenderer != null && highlightRenderer.material != null)
            originalMaterial = highlightRenderer.material;
    }

    public string InteractionPrompt
    {
        get
        {
            if (parentPot == null || !parentPot.isPlanted) return string.Empty;
            return GrowthStageUtil.GetStage(parentPot) switch
            {
                GrowthStage.Sprout => "Branch too young to break",
                GrowthStage.Early  => "Break Branch",
                GrowthStage.Late   => "Branch too hardened to break",
                GrowthStage.Mature => string.Empty,
                _ => string.Empty
            };
        }
    }

    public void Highlight(bool active)
    {
        if (highlightRenderer == null || highlightMaterial == null) return;
        highlightRenderer.material = active ? highlightMaterial : originalMaterial;
    }

    public void Interact()
    {
        if (parentPot == null || !GrowthStageUtil.CanBreakBranch(parentPot))
        {
            if (HUDManager.Instance != null)
                HUDManager.Instance.ShowMessage(InteractionPrompt);
            return;
        }

        string branchItemName = $"Branch:{parentPot.plantedSeedData.seedName}";
        InventoryManager.Instance?.AddItem(branchItemName, ItemType.Material, 1);

        float penalty = GraftConfig.Instance != null ? GraftConfig.Instance.branchBreakPenaltySeconds : 30f;
        parentPot.currentGrowthTime = Mathf.Max(0f, parentPot.currentGrowthTime - penalty);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowMessage($"Broke a branch from {parentPot.plantedSeedData.seedName}.");

        gameObject.SetActive(false);
    }
}
