using UnityEngine;

public class PlantPot : MonoBehaviour, IInteractable
{
    [Header("Plant Settings")]
    public bool isPlanted = false;
    public SeedData plantedSeedData;
    public GameObject currentPlant;

    [Header("Growth & Water")]
    public float currentGrowthTime = 0f;
    public float currentWaterLevel = 0f;     // water currently stored in the pot
    private float maxWaterCapacity = 0f;      // from SeedData.waterRequired
    private float waterConsumptionRate = 0f;  // maxWaterCapacity / growthTime

    public bool isReadyToHarvest = false;

    [Header("Visual Effects")]
    public Material highlightMaterial;
    public MeshRenderer potRenderer;
    private Material originalMaterial;

    [Header("References")]
    public Transform plantSpawnPoint;

    private string interactionPrompt = "Plant Seeds";
    public string InteractionPrompt => interactionPrompt;

    private void Start()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<MeshRenderer>();

        if (potRenderer != null && potRenderer.material != null)
            originalMaterial = potRenderer.material;
    }

    private void Update()
    {
        // Only progress growth if planted, not yet harvestable, and has water
        if (isPlanted && !isReadyToHarvest && plantedSeedData != null)
        {
            if (currentWaterLevel > 0f)
            {
                // Consume water over time
                float waterUsed = waterConsumptionRate * Time.deltaTime;
                currentWaterLevel = Mathf.Max(0f, currentWaterLevel - waterUsed);

                // Growth increases only when water is available
                currentGrowthTime += Time.deltaTime;

                // Update visual scale
                UpdatePlantGrowthVisual();

                // Check for full maturity
                if (currentGrowthTime >= plantedSeedData.growthTime)
                {
                    MakePlantReadyToHarvest();
                }
            }
            // else: no water → growth stops (timer pauses)
        }
    }

    /// <summary>
    /// Called by the WateringController when player left‑clicks this pot.
    /// </summary>
    /// <param name="waterAmount">Amount of water to add (from inventory)</param>
    public void WaterPlant(float waterAmount)
    {
        if (!isPlanted)
        {
            Debug.Log("Cannot water an empty pot!");
            return;
        }

        if (isReadyToHarvest)
        {
            Debug.Log("Plant is already fully grown!");
            return;
        }

        // Add water, but do not exceed max capacity
        float newWaterLevel = currentWaterLevel + waterAmount;
        currentWaterLevel = Mathf.Min(maxWaterCapacity, newWaterLevel);

        // Optional: show UI feedback
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"Watered {plantedSeedData.seedName}: {currentWaterLevel:F1}/{maxWaterCapacity:F1}");
        }
        else
        {
            Debug.Log($"Watered {plantedSeedData.seedName}. Water: {currentWaterLevel:F1}/{maxWaterCapacity:F1}");
        }
    }

    public void Interact()
    {
        if (isReadyToHarvest)
        {
            HarvestPlant();
        }
        else if (!isPlanted)
        {
            OpenPlantingUI();
        }
        else
        {
            // Show growth and water status
            float growthPercent = (currentGrowthTime / plantedSeedData.growthTime) * 100f;
            string status = $"{plantedSeedData.seedName} – {growthPercent:F0}% grown\n" +
                            $"Water: {currentWaterLevel:F1}/{maxWaterCapacity:F1}";
            if (HUDManager.Instance != null)
                HUDManager.Instance.ShowMessage(status);
            else
                Debug.Log(status);
        }
    }

    private void OpenPlantingUI()
    {
        if (PlantingUIManager.Instance != null)
            PlantingUIManager.Instance.ShowPlantingUI(this);
        else
            Debug.LogError("PlantingUIManager.Instance not found!");
    }

    public void PlantSeed(SeedData seedData)
    {
        if (isPlanted)
        {
            Debug.LogWarning("Pot already has a plant!");
            return;
        }

        // Check if player has the seed
        if (!SeedManager.Instance.HasSeed(seedData.seedName, 1))
        {
            Debug.Log($"No {seedData.seedName} seeds available!");
            if (HUDManager.Instance != null)
                HUDManager.Instance.ShowMessage($"No {seedData.seedName} seeds available!");
            return;
        }

        // Remove one seed
        SeedManager.Instance.RemoveSeeds(seedData.seedName, 1);

        // Set up plant data
        isPlanted = true;
        plantedSeedData = seedData;
        currentGrowthTime = 0f;
        isReadyToHarvest = false;
        interactionPrompt = "Water Plant";

        // Water parameters
        maxWaterCapacity = seedData.waterRequired;
        currentWaterLevel = 0f;                     // starts dry
        waterConsumptionRate = maxWaterCapacity / seedData.growthTime;

        // Spawn seedling
        if (seedData.seedlingPrefab != null && plantSpawnPoint != null)
        {
            currentPlant = Instantiate(seedData.seedlingPrefab, plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
            currentPlant.transform.localScale = Vector3.one * 0.3f;
        }
        else if (seedData.seedlingPrefab == null)
        {
            Debug.LogWarning($"No seedling prefab assigned for {seedData.seedName}");
        }

        Debug.Log($"Planted {seedData.seedName} in pot!");
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowMessage($"Planted {seedData.seedName}! It needs water to grow.");
    }

    private void UpdatePlantGrowthVisual()
    {
        if (currentPlant != null && plantedSeedData != null && !isReadyToHarvest)
        {
            float growthPercent = currentGrowthTime / plantedSeedData.growthTime;
            float scale = Mathf.Lerp(0.3f, 1f, growthPercent);
            currentPlant.transform.localScale = Vector3.one * scale;
        }
    }

    private void MakePlantReadyToHarvest()
    {
        isReadyToHarvest = true;
        interactionPrompt = "Harvest Plant";

        // Replace seedling with mature plant (if prefab exists)
        if (plantedSeedData.maturePlantPrefab != null && currentPlant != null)
        {
            Destroy(currentPlant);
            currentPlant = Instantiate(plantedSeedData.maturePlantPrefab, plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
        }
        else if (currentPlant != null)
        {
            // If no mature prefab, just scale to full size
            currentPlant.transform.localScale = Vector3.one;
        }

        // Unparent so the plant stays in world when pot is destroyed
        if (currentPlant != null)
            currentPlant.transform.SetParent(null);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowMessage($"{plantedSeedData.seedName} is ready to harvest!");

        Debug.Log($"{plantedSeedData.seedName} is ready to harvest!");

        // Destroy the pot itself (the plant is now independent)
        Destroy(gameObject);
    }

    private void HarvestPlant()
    {
        if (!isReadyToHarvest)
        {
            Debug.LogWarning("Plant is not ready to harvest yet!");
            return;
        }

        // Add harvest yield to inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(
                plantedSeedData.harvestItemName,
                plantedSeedData.harvestItemType,
                plantedSeedData.harvestYield
            );

            Debug.Log($"Harvested {plantedSeedData.harvestYield} {plantedSeedData.harvestItemName}!");
            if (HUDManager.Instance != null)
                HUDManager.Instance.ShowMessage($"Harvested {plantedSeedData.harvestYield} {plantedSeedData.harvestItemName}!");
        }
        else
        {
            Debug.LogError("InventoryManager.Instance not found!");
        }

        // Destroy the mature plant
        if (currentPlant != null)
            Destroy(currentPlant);

        // Pot is already destroyed after maturing, but if not, reset
        if (gameObject != null && !isReadyToHarvest) // safety
            ResetPot();
    }

    private void ResetPot()
    {
        if (currentPlant != null)
            Destroy(currentPlant);

        isPlanted = false;
        plantedSeedData = null;
        currentGrowthTime = 0f;
        currentWaterLevel = 0f;
        maxWaterCapacity = 0f;
        waterConsumptionRate = 0f;
        isReadyToHarvest = false;
        interactionPrompt = "Press F to Plant Seeds";
    }

    public void Highlight(bool highlight)
    {
        if (potRenderer != null && highlightMaterial != null)
            potRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }
}
