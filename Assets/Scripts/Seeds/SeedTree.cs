using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(TreeOxygenArea))]
public class SeedTree : MonoBehaviour, IInteractable
{
    [Header("Tree Settings")]
    [SerializeField] private string treeName = "Oak Tree";
    [SerializeField] private string seedType = "Acorn";
    [SerializeField] private int seedAmount = 3;
    [Header("Seed Data (optional)")]
    [SerializeField] private SeedData seedData;
    [SerializeField] private float interactionCooldown = 5f; // Time before tree can be interacted again

    [Header("Reset Timer")]
    [SerializeField] private float resetTime = 60f; // Time for seeds to regrow
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

    // Harvest tracking for diminishing returns and flower seeds
    private int harvestCount = 0;  // Total harvests in this cycle
    private int regrowCycleCount = 0;  // How many regrow cycles completed
    private float[] harvestReductionMultipliers;

    private int adjustedSeedAmount
    {
        get
        {
            if (seedData == null || harvestReductionMultipliers == null || harvestReductionMultipliers.Length == 0)
                return seedAmount;

            int clampedHarvestCount = Mathf.Clamp(harvestCount, 0, harvestReductionMultipliers.Length - 1);
            return Mathf.Max(1, Mathf.RoundToInt(seedData.harvestYield * harvestReductionMultipliers[clampedHarvestCount]));
        }
    }

    public string InteractionPrompt
    {
        get
        {
            if (!canInteract)
            {
                float timeRemaining = interactionCooldown - (Time.time - lastInteractionTime);
                if (timeRemaining > 0)
                {
                    return $"Tree is shaking... Wait {Mathf.CeilToInt(timeRemaining)}s";
                }
            }

            if (isRegrowing)
            {
                if (showTimerInPrompt)
                {
                    float timeRemaining = resetTime - (Time.time - regrowStartTime);
                    if (timeRemaining > 0)
                    {
                        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                        return $"Tree is regrowing. Ready in {minutes:00}:{seconds:00}";
                    }
                }
                return "Tree is regrowing...";
            }

            // Get the adjusted seed amount (with diminishing returns applied)
            string seed = seedData != null ? seedData.seedName : seedType;

            // Show if this will be a flower seed harvest
            if (seedData != null && seedData.producesFlowerSeeds && regrowCycleCount >= seedData.maxHarvests - 1)
            {
                return $"Press F to collect FLOWER SEEDS from {treeName}";
            }

            return $"Press F to collect {adjustedSeedAmount} {seed}(s) from {treeName}";
        }
    }

    private void Start()
    {
        treeRenderer = GetComponent<Renderer>();

        if (treeRenderer != null)
        {
            originalMaterial = treeRenderer.material;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && seedCollectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        isRegrowing = false;
        canInteract = true;
        harvestCount = 0;
        regrowCycleCount = 0;

        // Initialize harvest reduction multipliers
        int maxHarvests = (seedData != null) ? seedData.maxHarvests : 1;
        harvestReductionMultipliers = new float[maxHarvests];
        for (int i = 0; i < maxHarvests; i++)
        {
            harvestReductionMultipliers[i] = Mathf.Max(0.3f, 1.0f - (i * 0.35f));
        }

        // Activate the oxygen area
        TreeOxygenArea oxygen = GetComponent<TreeOxygenArea>();
        if (oxygen != null && seedData != null)
        {
            oxygen.Setup(seedData);
        }
    }

    private void Update()
    {
        // Check if cooldown is over
        if (!canInteract && Time.time >= lastInteractionTime + interactionCooldown)
        {
            canInteract = true;
        }

        // Check if tree should finish regrowing
        if (isRegrowing && Time.time >= regrowStartTime + resetTime)
        {
            FinishRegrow();
        }
    }

    public void Highlight(bool active)
    {
        if (treeRenderer == null) return;

        if (active && !isRegrowing && canInteract && highlightMaterial != null)
        {
            treeRenderer.material = highlightMaterial;
        }
        else
        {
            if (originalMaterial != null)
                treeRenderer.material = originalMaterial;
        }
    }

    public void Interact()
    {
        // Check if tree is regrowing
        if (isRegrowing)
        {
            float timeRemaining = resetTime - (Time.time - regrowStartTime);
            if (timeRemaining > 0)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                string message = $"Tree is regrowing. Ready in {minutes:00}:{seconds:00}";
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.ShowMessage(message, 2f);
                }
            }
            return;
        }

        // Check cooldown
        if (!canInteract)
        {
            float timeRemaining = interactionCooldown - (Time.time - lastInteractionTime);
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage($"Wait {Mathf.CeilToInt(timeRemaining)}s", 1.5f);
            }
            return;
        }

        // Collect seeds
        CollectSeeds();

        // Increment harvest count AFTER collecting
        harvestCount++;

        // Start cooldown
        canInteract = false;
        lastInteractionTime = Time.time;

        // Start regrow
        StartRegrow();

        // Play effects
        PlayCollectEffects();

        // Shake tree animation
        StartCoroutine(ShakeTree());

        // Visual feedback
        StartCoroutine(FlashGreen());
    }

    private void CollectSeeds()
    {
        // If tree produces flower seeds and we've reached max regrow cycles, collect flower seeds
        if (seedData != null && seedData.producesFlowerSeeds && regrowCycleCount >= seedData.maxHarvests - 1)
        {
            CollectFlowerSeeds();
            return;
        }

        // Use SeedTreeData if assigned, otherwise fallback to seedType/seedAmount
        if (seedData == null)
        {
            Debug.Log($"<color=yellow>SeedTree '{treeName}' is missing SeedData reference. Using fallback values.</color>");
        }
        else
        {
            Debug.Log($"<color=cyan>Collecting seeds from '{treeName}' using SeedData: {seedData.seedName} x{seedData.harvestYield}</color>");
        }

        string nameToAdd = seedData != null ? seedData.seedName : seedType;
        int baseAmount = seedData != null ? seedData.harvestYield : seedAmount;

        // Apply diminishing returns with randomization
        float multiplier = harvestReductionMultipliers[harvestCount];
        int adjustedAmount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier));

        // Add randomness (±20% of the adjusted amount)
        int randomVariance = Mathf.RoundToInt(adjustedAmount * 0.2f);
        adjustedAmount = Random.Range(adjustedAmount - randomVariance, adjustedAmount + randomVariance + 1);
        adjustedAmount = Mathf.Max(1, adjustedAmount);

        Debug.Log($"<color=magenta>Adding seeds: {nameToAdd} x{adjustedAmount} (Harvest {harvestCount + 1}, Regrow Cycle: {regrowCycleCount + 1}, Multiplier: {multiplier:F2})</color>");

        if (SeedManager.Instance != null)
        {
            SeedManager.Instance.AddSeeds(nameToAdd, adjustedAmount);

            string message = $"<color=green>Collected {adjustedAmount} {nameToAdd}(s)! (Cycle {regrowCycleCount + 1})</color>";
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(message, 2f);
            }
            Debug.Log(message);
            return;
        }

        // Fallback to InventoryManager if SeedManager isn't available
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(nameToAdd, ItemType.Seed, adjustedAmount);

            string message = $"<color=green>Collected {adjustedAmount} {nameToAdd}(s)! (Cycle {regrowCycleCount + 1})</color>";
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(message, 2f);
            }
            Debug.Log(message);
        }
    }

    private void CollectFlowerSeeds()
    {
        if (seedData == null || seedData.flowerSeedYields == null || seedData.flowerSeedYields.Count == 0)
        {
            Debug.LogWarning($"<color=red>SeedTree '{treeName}' is configured to produce flower seeds but has no flower seed data.</color>");
            return;
        }

        // Randomly select a flower seed type
        FlowerSeedYield selectedFlowerYield = seedData.flowerSeedYields[Random.Range(0, seedData.flowerSeedYields.Count)];

        if (selectedFlowerYield.flowerSeedData == null)
        {
            Debug.LogWarning($"<color=red>SeedTree '{treeName}' has empty FlowerSeedData reference in flowerSeedYields.</color>");
            return;
        }

        // Calculate amount with randomization
        int amount = Random.Range(selectedFlowerYield.minYield, selectedFlowerYield.maxYield + 1);

        Debug.Log($"<color=cyan>Collecting flower seeds from '{treeName}': {selectedFlowerYield.flowerSeedData.seedName} x{amount} (After {regrowCycleCount} regrow cycles)</color>");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(selectedFlowerYield.flowerSeedData.seedName, ItemType.FlowerSeeds, amount);

            string message = $"<color=magenta>Collected {amount} {selectedFlowerYield.flowerSeedData.seedName}(s)!</color>";
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(message, 2f);
            }
            Debug.Log(message);
        }
    }

    private void StartRegrow()
    {
        isRegrowing = true;
        regrowStartTime = Time.time;
        regrowCycleCount++; // Increment cycle count

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"Tree is regrowing...", 2f);
        }

        Debug.Log($"<color=yellow>Tree '{treeName}' started regrowing. Regrow cycle: {regrowCycleCount}, Next harvest will be harvest #{harvestCount + 1}</color>");
    }

    private void FinishRegrow()
    {
        isRegrowing = false;
        canInteract = true;

        // Reset visual
        if (treeRenderer != null && originalMaterial != null)
        {
            treeRenderer.material = originalMaterial;
            treeRenderer.material.color = Color.white;
        }

        string message = $"{treeName} has grown new seeds! (Cycle {regrowCycleCount})";
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage(message, 3f);
        }

        Debug.Log(message);
    }

    private void PlayCollectEffects()
    {
        // Play particle effect
        if (seedCollectEffect != null)
        {
            seedCollectEffect.Play();
        }

        // Play sound
        if (seedCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(seedCollectSound);
        }
    }

    private IEnumerator ShakeTree()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float shakeAmount = 0.05f;

        while (elapsed < 0.3f)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
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
            {
                // Make tree slightly darker to show it's regrowing
                treeRenderer.material.color = new Color(0.4f, 0.3f, 0.2f);
            }
            else if (originalMaterial != null)
            {
                treeRenderer.material = originalMaterial;
            }
        }
    }

    // Optional: Show regrow time in OnDrawGizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (isRegrowing)
        {
            Gizmos.color = Color.yellow;
            float timeRemaining = resetTime - (Time.time - regrowStartTime);
            if (timeRemaining > 0)
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
    }
}
