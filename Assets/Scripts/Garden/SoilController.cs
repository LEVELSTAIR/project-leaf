using UnityEngine;

public class SoilController : MonoBehaviour, IInteractable
{
    [Header("Soil Settings")]
    [SerializeField] private string soilName = "Flower Bed";
    [SerializeField] private Transform plantSpawnPoint;

    [Header("Visual Effects")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private ParticleSystem plantEffect;
    [SerializeField] private AudioClip plantSound;

    private Renderer soilRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;
    private bool hasFlower = false;
    private GameObject currentFlower;
    private FlowerSeedData plantedFlowerData;

    public string InteractionPrompt
    {
        get
        {
            if (hasFlower)
            {
                return $"Flower bed has {plantedFlowerData.seedName}. Press F to harvest.";
            }
            return "Press F to plant a flower seed";
        }
    }

    private void Start()
    {
        soilRenderer = GetComponent<Renderer>();
        if (soilRenderer != null)
        {
            originalMaterial = soilRenderer.material;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && plantSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // If no spawn point assigned, use this object's position
        if (plantSpawnPoint == null)
        {
            plantSpawnPoint = transform;
        }
    }

    public void Highlight(bool active)
    {
        if (soilRenderer == null) return;

        if (active && highlightMaterial != null)
        {
            soilRenderer.material = highlightMaterial;
        }
        else if (originalMaterial != null)
        {
            soilRenderer.material = originalMaterial;
        }
    }

    public void Interact()
    {
        if (hasFlower)
        {
            HarvestFlower();
        }
        else
        {
            ShowFlowerSeedUI();
        }
    }

    private void ShowFlowerSeedUI()
    {
        if (FlowerSeedPlantingUIManager.Instance != null)
        {
            FlowerSeedPlantingUIManager.Instance.ShowUI(this);
        }
    }

    public void PlantFlowerSeed(FlowerSeedData flowerSeedData, int amountToConsume)
    {
        if (flowerSeedData == null)
        {
            Debug.LogError($"SoilController '{soilName}': Cannot plant null flower seed data!");
            return;
        }

        if (hasFlower)
        {
            Debug.LogWarning($"SoilController '{soilName}': Already has a flower planted!");
            return;
        }

        // Remove flower seeds from inventory
        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(flowerSeedData.seedName, ItemType.FlowerSeeds, amountToConsume))
            {
                Debug.LogWarning($"SoilController '{soilName}': Not enough {flowerSeedData.seedName} in inventory!");
                return;
            }

            InventoryManager.Instance.RemoveItem(flowerSeedData.seedName, ItemType.FlowerSeeds, amountToConsume);
            Debug.Log($"Consumed {amountToConsume} {flowerSeedData.seedName}(s) from inventory");
        }

        // Spawn the flower
        if (flowerSeedData.flowerPrefab != null)
        {
            // Instantiate without parent first to preserve original scale
            currentFlower = Instantiate(flowerSeedData.flowerPrefab, plantSpawnPoint.position, Quaternion.identity);

            // Then set parent and adjust position to account for parent's offset
            currentFlower.transform.SetParent(plantSpawnPoint);
            currentFlower.transform.localPosition = Vector3.zero;

            Debug.Log($"<color=green>Planted {flowerSeedData.seedName} at {soilName}</color>");
        }

        plantedFlowerData = flowerSeedData;
        hasFlower = true;

        // Play effects
        PlayPlantEffects();

        // Show message
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"Planted {flowerSeedData.seedName}!", 2f);
        }
    }

    private Vector3 GetWorldScale(Transform t)
    {
        return new Vector3(
            t.lossyScale.x,
            t.lossyScale.y,
            t.lossyScale.z
        );
    }

    private void HarvestFlower()
    {
        if (!hasFlower || currentFlower == null)
        {
            Debug.LogWarning($"SoilController '{soilName}': No flower to harvest!");
            return;
        }

        Debug.Log($"<color=yellow>Harvested {plantedFlowerData.seedName} from {soilName}</color>");

        // Destroy the flower
        Destroy(currentFlower);
        hasFlower = false;
        plantedFlowerData = null;

        // Show message
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"Harvested flower!", 2f);
        }
    }

    private void PlayPlantEffects()
    {
        if (plantEffect != null)
        {
            plantEffect.Play();
        }

        if (plantSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(plantSound);
        }
    }

    public bool HasFlower() => hasFlower;
    public FlowerSeedData GetPlantedFlower() => plantedFlowerData;
}
