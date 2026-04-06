// PlantPot.cs
using UnityEngine;

public class PlantPot : MonoBehaviour, IInteractable
{
    [Header("Plant Settings")]
    public bool isPlanted = false;
    public SeedData plantedSeedData;
    public GameObject currentPlant;

    [Header("Growth Settings")]
    public float currentGrowthTime = 0f;
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
        if (isPlanted && !isReadyToHarvest)
        {
            currentGrowthTime += Time.deltaTime;

            // Update plant growth visual
            UpdatePlantGrowthVisual();

            // Check if plant is fully grown
            if (currentGrowthTime >= plantedSeedData.growthTime)
            {
                MakePlantReadyToHarvest();
            }
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
            // Show growth progress
            float progress = (currentGrowthTime / plantedSeedData.growthTime) * 100f;
            string progressText = $"{plantedSeedData.seedName} growing: {progress:F0}%";

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(progressText);
            }
            else
            {
                Debug.Log(progressText);
            }
        }
    }

    private void OpenPlantingUI()
    {
        if (PlantingUIManager.Instance != null)
        {
            PlantingUIManager.Instance.ShowPlantingUI(this);
        }
        else
        {
            Debug.LogError("PlantingUIManager.Instance not found!");
        }
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
            {
                HUDManager.Instance.ShowMessage($"No {seedData.seedName} seeds available!");
            }
            return;
        }

        // Remove one seed from inventory
        SeedManager.Instance.RemoveSeeds(seedData.seedName, 1);

        // Plant the seed
        isPlanted = true;
        plantedSeedData = seedData;
        currentGrowthTime = 0f;
        isReadyToHarvest = false;
        interactionPrompt = "Check Plant";

        // Spawn seedling
        if (seedData.seedlingPrefab != null && plantSpawnPoint != null)
        {
            currentPlant = Instantiate(seedData.seedlingPrefab, plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
            // Start with small scale
            currentPlant.transform.localScale = Vector3.one * 0.3f;
        }
        else if (seedData.seedlingPrefab == null)
        {
            Debug.LogWarning($"No seedling prefab assigned for {seedData.seedName}");
        }

        Debug.Log($"Planted {seedData.seedName} in pot!");
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"Planted {seedData.seedName}!");
        }
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

        // Replace seedling with mature plant if prefab exists
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

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"{plantedSeedData.seedName} is ready to harvest!");
        }

        Debug.Log($"{plantedSeedData.seedName} is ready to harvest!");

        // Unparent the plant so it's no longer a child of the pot
        if (currentPlant != null)
        {
            currentPlant.transform.SetParent(null);
            Debug.Log($"Plant {plantedSeedData.seedName} is now independent in the world");
        }

        // Destroy the pot after plant matures
        Debug.Log($"Destroying pot - plant {plantedSeedData.seedName} has matured");
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
            {
                HUDManager.Instance.ShowMessage($"Harvested {plantedSeedData.harvestYield} {plantedSeedData.harvestItemName}!");
            }
        }
        else
        {
            Debug.LogError("InventoryManager.Instance not found!");
        }

        // Destroy the mature plant after harvesting
        if (currentPlant != null)
        {
            Destroy(currentPlant);
            Debug.Log($"Harvested and removed {plantedSeedData.seedName} from the world");
        }
    }

    private void ResetPot()
    {
        // Destroy current plant object
        if (currentPlant != null)
        {
            Destroy(currentPlant);
            currentPlant = null;
        }

        // Reset variables
        isPlanted = false;
        plantedSeedData = null;
        currentGrowthTime = 0f;
        isReadyToHarvest = false;
        interactionPrompt = "Plant Seeds";
    }

    public void Highlight(bool highlight)
    {
        if (potRenderer != null && highlightMaterial != null)
        {
            potRenderer.material = highlight ? highlightMaterial : originalMaterial;
        }
    }

}
