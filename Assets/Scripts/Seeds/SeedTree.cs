using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

//[RequireComponent(typeof(TreeOxygenArea))]
public class SeedTree : MonoBehaviour
{
    [Header("Tree Settings")]
    [SerializeField] private string treeName = "Oak Tree";
    [SerializeField] private string seedType = "Acorn";
    [SerializeField] private int seedAmount = 3;
    
    [Header("Seed Data (optional)")]
    [SerializeField] private SeedData seedData;
    [SerializeField] private float interactionCooldown = 5f;

    [Header("Reset Timer")]
    [SerializeField] private float resetTime = 60f;
    [SerializeField] private bool showTimerInPrompt = true;

    [Header("Visual Effects")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private ParticleSystem seedCollectEffect;
    [SerializeField] private AudioClip seedCollectSound;

    private Renderer treeRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;
    private bool isRegrowing = false;
    private float regrowStartTime;
    private float lastInteractionTime;
    private bool canInteract = true;

    private int harvestCount = 0;
    private int regrowCycleCount = 0;
    private float[] harvestReductionMultipliers;

    private int adjustedSeedAmount
    {
        get
        {
            if (seedData == null || harvestReductionMultipliers == null || harvestReductionMultipliers.Length == 0)
                return seedAmount;

            int clamped = Mathf.Clamp(harvestCount, 0, harvestReductionMultipliers.Length - 1);
            return Mathf.Max(1, Mathf.RoundToInt(seedData.harvestYield * harvestReductionMultipliers[clamped]));
        }
    }

    private void Start()
    {
        treeRenderer = GetComponent<Renderer>();
        if (treeRenderer != null) originalMaterial = treeRenderer.material;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && seedCollectSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        isRegrowing = false;
        canInteract = true;
        harvestCount = 0;
        regrowCycleCount = 0;

        int maxHarvests = (seedData != null) ? seedData.maxHarvests : 1;
        harvestReductionMultipliers = new float[maxHarvests];
        for (int i = 0; i < maxHarvests; i++)
            harvestReductionMultipliers[i] = Mathf.Max(0.3f, 1.0f - (i * 0.35f));

        TreeOxygenArea oxygen = GetComponent<TreeOxygenArea>();
        if (oxygen != null && seedData != null)
            oxygen.Setup(seedData);
    }

    private void Update()
    {
        if (!canInteract && Time.time >= lastInteractionTime + interactionCooldown)
            canInteract = true;

        if (isRegrowing && Time.time >= regrowStartTime + resetTime)
            FinishRegrow();
    }

    public void CollectSeeds()
    {
        if (isRegrowing)
        {
            float remaining = resetTime - (Time.time - regrowStartTime);
            if (remaining > 0)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                HUDManager.Instance?.ShowMessage($"Tree is regrowing. Ready in {minutes:00}:{seconds:00}", 2f);
            }
            return;
        }

        if (!canInteract)
        {
            float remaining = interactionCooldown - (Time.time - lastInteractionTime);
            HUDManager.Instance?.ShowMessage($"Wait {Mathf.CeilToInt(remaining)}s", 1.5f);
            return;
        }

        // Perform collection
        CollectSeedsInternal();

        harvestCount++;
        canInteract = false;
        lastInteractionTime = Time.time;
        StartRegrow();

        PlayCollectEffects();
        StartCoroutine(ShakeTree());
        StartCoroutine(FlashGreen());
    }

    private void CollectSeedsInternal()
    {
        // Flower seed harvest?
        if (seedData != null && seedData.producesFlowerSeeds && regrowCycleCount >= seedData.maxHarvests - 1)
        {
            CollectFlowerSeeds();
            return;
        }

        string nameToAdd = seedData != null ? seedData.seedName : seedType;
        int baseAmount = seedData != null ? seedData.harvestYield : seedAmount;

        float multiplier = harvestReductionMultipliers[harvestCount];
        int adjusted = Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier));

        int variance = Mathf.RoundToInt(adjusted * 0.2f);
        adjusted = Random.Range(adjusted - variance, adjusted + variance + 1);
        adjusted = Mathf.Max(1, adjusted);

        if (SeedManager.Instance != null)
            SeedManager.Instance.AddSeeds(nameToAdd, adjusted);
        else if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(nameToAdd, ItemType.Seed, adjusted);

        HUDManager.Instance?.ShowMessage($"<color=green>+{adjusted} {nameToAdd}(s)! (Cycle {regrowCycleCount + 1})</color>", 2f);
        Debug.Log($"<color=magenta>Added {nameToAdd} x{adjusted}</color>");
    }

    private void CollectFlowerSeeds()
    {
        if (seedData == null || seedData.flowerSeedYields == null || seedData.flowerSeedYields.Count == 0)
        {
            Debug.LogWarning($"<color=red>SeedTree '{treeName}' has no flower seed data.</color>");
            return;
        }

        FlowerSeedYield selected = seedData.flowerSeedYields[Random.Range(0, seedData.flowerSeedYields.Count)];
        if (selected.flowerSeedData == null)
        {
            Debug.LogWarning($"<color=red>SeedTree '{treeName}' missing FlowerSeedData.</color>");
            return;
        }

        int amount = Random.Range(selected.minYield, selected.maxYield + 1);
        InventoryManager.Instance?.AddItem(selected.flowerSeedData.seedName, ItemType.FlowerSeeds, amount);
        HUDManager.Instance?.ShowMessage($"<color=magenta>+{amount} {selected.flowerSeedData.seedName}(s)!</color>", 2f);
    }

    private void StartRegrow()
    {
        isRegrowing = true;
        regrowStartTime = Time.time;
        regrowCycleCount++;
        HUDManager.Instance?.ShowMessage("Tree is regrowing...", 2f);
        Debug.Log($"<color=yellow>Tree '{treeName}' regrowing. Cycle: {regrowCycleCount}</color>");
    }

    private void FinishRegrow()
    {
        isRegrowing = false;
        canInteract = true;
        if (treeRenderer != null && originalMaterial != null)
        {
            treeRenderer.material = originalMaterial;
            treeRenderer.material.color = Color.white;
        }
        HUDManager.Instance?.ShowMessage($"{treeName} has grown new seeds! (Cycle {regrowCycleCount})", 3f);
    }

    private void PlayCollectEffects()
    {
        if (seedCollectEffect != null) seedCollectEffect.Play();
        if (seedCollectSound != null && audioSource != null)
            SoundManager.Instance.PlaySFXOneShot(seedCollectSound);
    }

    private IEnumerator ShakeTree()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = originalPos + Random.insideUnitSphere * 0.05f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }

    private IEnumerator FlashGreen()
    {
        if (treeRenderer != null)
        {
            treeRenderer.material.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            if (isRegrowing)
                treeRenderer.material.color = new Color(0.4f, 0.3f, 0.2f);
            else if (originalMaterial != null)
                treeRenderer.material = originalMaterial;
        }
    }

    // Called by the router to get the prompt
    public string GetInteractionPrompt()
    {
        if (!canInteract)
        {
            float remaining = interactionCooldown - (Time.time - lastInteractionTime);
            if (remaining > 0)
                return $"Tree is shaking... Wait {Mathf.CeilToInt(remaining)}s";
        }

        if (isRegrowing)
        {
            if (showTimerInPrompt)
            {
                float remaining = resetTime - (Time.time - regrowStartTime);
                if (remaining > 0)
                {
                    int minutes = Mathf.FloorToInt(remaining / 60f);
                    int seconds = Mathf.FloorToInt(remaining % 60f);
                    return $"Tree is regrowing. Ready in {minutes:00}:{seconds:00}";
                }
            }
            return "Tree is regrowing...";
        }

        string seed = seedData != null ? seedData.seedName : seedType;

        if (seedData != null && seedData.producesFlowerSeeds && regrowCycleCount >= seedData.maxHarvests - 1)
            return $"Press F to collect FLOWER SEEDS from {treeName}";

        return $"Press F to collect {adjustedSeedAmount} {seed}(s) from {treeName}";
    }

    public void Highlight(bool active)
    {
        if (treeRenderer == null) return;
        if (active && !isRegrowing && canInteract && highlightMaterial != null)
            treeRenderer.material = highlightMaterial;
        else if (originalMaterial != null)
            treeRenderer.material = originalMaterial;
    }
}