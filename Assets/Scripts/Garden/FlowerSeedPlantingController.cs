using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FlowerSeedPlantingController : MonoBehaviour
{
    public static FlowerSeedPlantingController Instance { get; private set; }

    [Header("Placement Settings")]
    public LayerMask placementMask;
    public float placementMaxDistance = 50f;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Debug Settings")]
    public bool showPlacementGizmos = true;
    public float gizmoSize = 0.5f;
    public Camera placementCamera;

    private bool isPlacing = false;
    private GameObject placementPreview;
    private FlowerSeedData currentFlowerSeed;
    private SoilController targetSoil;
    private Camera mainCamera;

    // Gizmo tracking
    private Vector3 lastPreviewPosition = Vector3.zero;
    private bool lastRaycastHit = false;

    // Input System references
    private Mouse mouse;
    private Keyboard keyboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Use assigned camera, or fall back to Camera.main
        if (placementCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("FlowerSeedPlantingController: No camera assigned and Camera.main not found!");
            }
            else
            {
                Debug.Log("FlowerSeedPlantingController: Using Camera.main");
            }
        }
        else
        {
            mainCamera = placementCamera;
            Debug.Log($"FlowerSeedPlantingController: Using assigned camera: {mainCamera.name}");
        }

        mouse = Mouse.current;
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (isPlacing)
        {
            HandlePlacement();
        }
    }

    public void StartPlantingMode(FlowerSeedData flowerSeed, SoilController soil, int amountToConsume)
    {
        if (flowerSeed == null || soil == null)
        {
            Debug.LogError("FlowerSeedPlantingController: Invalid flower seed or soil!");
            return;
        }

        if (soil.HasFlower())
        {
            Debug.LogWarning("FlowerSeedPlantingController: Soil already has a flower!");
            return;
        }

        currentFlowerSeed = flowerSeed;
        targetSoil = soil;

        if (flowerSeed.flowerPrefab == null)
        {
            Debug.LogError("FlowerSeedPlantingController: Flower seed has no flower prefab!");
            return;
        }

        placementPreview = Instantiate(flowerSeed.flowerPrefab);

        if (placementPreview == null)
        {
            Debug.LogError("FlowerSeedPlantingController: Failed to instantiate preview!");
            return;
        }

        RemoveCollidersFromPreview(placementPreview);
        SetPreviewMaterial(placementPreview, validPlacementMaterial);

        // Ensure preview is active and visible
        placementPreview.SetActive(true);

        isPlacing = true;
        FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

        // Show placement instructions in HUD
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage("Left Click to Plant | X to Cancel", 999f);
        }

        Debug.Log($"FlowerSeedPlantingController: Started planting mode for {flowerSeed.seedName}");
    }

    private void HandlePlacement()
    {
        if (placementPreview == null || mainCamera == null)
        {
            Debug.LogWarning($"FlowerSeedPlantingController: Placement cancelled - Preview null: {placementPreview == null}, Camera null: {mainCamera == null}");
            CancelPlanting();
            return;
        }

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placementMaxDistance, placementMask))
        {
            placementPreview.transform.position = hit.point;
            placementPreview.transform.rotation = Quaternion.identity;
            lastPreviewPosition = hit.point;
            lastRaycastHit = true;

            bool isValid = IsPlacementValid(hit);
            UpdatePreviewMaterial(isValid ? validPlacementMaterial : invalidPlacementMaterial);

            // Left mouse button to confirm placement
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && isValid)
            {
                ConfirmPlanting(hit.point);
            }
        }
        else
        {
            // Fallback: position preview in front of camera along the ray
            Vector3 fallbackPosition = ray.origin + ray.direction * (placementMaxDistance * 0.5f);
            placementPreview.transform.position = fallbackPosition;
            lastPreviewPosition = fallbackPosition;
            lastRaycastHit = false;
            UpdatePreviewMaterial(invalidPlacementMaterial);
        }

        // Cancel with X key or Right Mouse Button
        if (keyboard != null && keyboard[Key.X].wasPressedThisFrame)
        {
            CancelPlanting();
        }
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            CancelPlanting();
        }
    }

    private bool IsPlacementValid(RaycastHit hit)
    {
        // Check if we hit the target soil
        SoilController soil = hit.collider.GetComponent<SoilController>();
        if (soil != null && soil == targetSoil && !soil.HasFlower())
        {
            return true;
        }

        return false;
    }

    private void ConfirmPlanting(Vector3 position)
    {
        if (targetSoil != null && currentFlowerSeed != null)
        {
            // Get amount to consume (usually 1, but configurable)
            int amountToConsume = 1;

            // Plant the flower
            targetSoil.PlantFlowerSeed(currentFlowerSeed, amountToConsume);

            // Show success message
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage($"{currentFlowerSeed.seedName} planted successfully!", 2f);
            }

            Debug.Log($"<color=green>Successfully planted {currentFlowerSeed.seedName}</color>");
        }

        Destroy(placementPreview);
        isPlacing = false;
        currentFlowerSeed = null;
        targetSoil = null;

        // Close UI and sync cursor state
        FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SyncCursorState();
        }
    }

    public void CancelPlanting()
    {
        if (placementPreview != null)
            Destroy(placementPreview);

        isPlacing = false;
        currentFlowerSeed = null;
        targetSoil = null;

        // Hide placement message
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage("Planting cancelled", 2f);
        }

        // Close UI and sync cursor state
        FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SyncCursorState();
        }

        Debug.Log("Planting cancelled");
    }

    private void RemoveCollidersFromPreview(GameObject obj)
    {
        foreach (var col in obj.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }
    }

    private void SetPreviewMaterial(GameObject obj, Material mat)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        Debug.Log($"FlowerSeedPlantingController: Setting preview material on {renderers.Length} renderers. Material: {(mat != null ? mat.name : "null")}");

        if (mat == null)
        {
            Debug.LogWarning("FlowerSeedPlantingController: Material is null! Creating fallback material.");
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }

        foreach (var renderer in renderers)
        {
            renderer.material = mat;
        }
    }

    private void UpdatePreviewMaterial(Material mat)
    {
        if (placementPreview == null) return;

        if (mat == null)
        {
            Debug.LogWarning("FlowerSeedPlantingController: UpdatePreviewMaterial - Material is null! Using fallback.");
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0.5f, 0.5f);
        }

        var renderers = placementPreview.GetComponentsInChildren<Renderer>();
        Debug.Log($"FlowerSeedPlantingController: Updating preview material on {renderers.Length} renderers. Position: {placementPreview.transform.position}, Material: {mat.name}");

        foreach (var renderer in renderers)
        {
            renderer.material = mat;
        }
    }

    public bool IsCurrentlyPlacing() => isPlacing;

    private void OnDrawGizmos()
    {
        if (!showPlacementGizmos || !isPlacing)
            return;

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null)
            return;

        Gizmos.color = lastRaycastHit ? Color.blue : new Color(1f, 0.5f, 0f);
        Gizmos.DrawSphere(lastPreviewPosition, gizmoSize);

        Gizmos.DrawWireCube(lastPreviewPosition, Vector3.one * gizmoSize * 2f);

        Gizmos.color = lastRaycastHit ? Color.blue : new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(cameraToUse.transform.position, lastPreviewPosition);
    }
}
