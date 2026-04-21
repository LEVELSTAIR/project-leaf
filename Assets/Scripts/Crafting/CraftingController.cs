// CraftingController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/*
 A centralized crafting controller responsible for runtime behavior of
 the crafting system.

 Responsibilities:
 - Accepts crafting requests (`TryCraftRecipe`) and verifies resource
   availability via `CraftingRecipe.CanCraft`.
 - Consumes resources and starts a placement flow for the crafted
   object (`StartPlacement` / `HandlePlacement`).
 - Instantiates the preview object, removes its colliders and applies
   preview materials (valid/invalid) for visual feedback.
 - Positions the preview using a camera-forward raycast (configurable
   camera) and falls back to a point in front of the camera when the
   raycast misses.
 - Confirms placement, instantiating the final object and optionally
   parenting it under the recipe's `parentTransform` for scene
   organization.
 - Provides debugging aids: placement gizmos and detailed log messages.
 - Integrates with other systems:
     * `InventoryManager` for resource checks/consumption
     * `CraftingUIManager` to open/close crafting UI
     * `KeyboardInputManager` to coordinate cursor state
     * `HUDManager` to display placement messages and feedback

 Usage notes / tips:
 - Assign `placementCamera` in the inspector to control which camera
   is used for placement rays and gizmo visualization. If left empty,
   the controller falls back to `Camera.main`.
 - Configure `placementMask` and `placementMaxDistance` to control
   where items can be placed in the world.
 - Provide `validPlacementMaterial` and `invalidPlacementMaterial` to
   clearly indicate valid/invalid placement areas.
 - Use `parentTransform` on each `CraftingRecipe` asset to keep spawned
   objects organized under a dedicated GameObject in the scene.
 - Toggle `showPlacementGizmos` during development to visualize the
   placement ray and target point in the Scene view.

 This controller is intentionally data-driven: most per-recipe
 configuration lives in `CraftingRecipe` ScriptableObjects so new
 craftables can be defined without code changes.
 */

public class CraftingController : MonoBehaviour
{
    public static CraftingController Instance { get; private set; }

    [Header("Crafting Settings")]
    public List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();

    [Header("Placement Settings")]
    public LayerMask placementMask;
    public float placementMaxDistance = 50f;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Debug Settings")]
    public bool showPlacementGizmos = true;
    public float gizmoSize = 0.5f;
    public Camera placementCamera;

    // Events
    public System.Action<CraftingRecipe> OnCraftingSuccess;
    public System.Action<CraftingRecipe> OnCraftingFailed;

    private bool isPlacing = false;
    private GameObject placementPreview;
    private CraftingRecipe currentRecipe;
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
                Debug.LogError("CraftingController: No camera assigned and Camera.main not found!");
            }
            else
            {
                Debug.Log("CraftingController: Using Camera.main");
            }
        }
        else
        {
            mainCamera = placementCamera;
            Debug.Log($"CraftingController: Using assigned camera: {mainCamera.name}");
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

    public void TryCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return;

        if (!recipe.CanCraft(InventoryManager.Instance))
        {
            Debug.Log($"Cannot craft {recipe.recipeName}: insufficient resources.");
            OnCraftingFailed?.Invoke(recipe);
            return;
        }

        recipe.ConsumeResources(InventoryManager.Instance);
        StartPlacement(recipe);
        OnCraftingSuccess?.Invoke(recipe);
    }

    private void StartPlacement(CraftingRecipe recipe)
    {
        currentRecipe = recipe;
        
        if (recipe.resultPrefab == null)
        {
            Debug.LogError("CraftingController: Recipe has no resultPrefab!");
            return;
        }
        
        placementPreview = Instantiate(recipe.resultPrefab);
        
        if (placementPreview == null)
        {
            Debug.LogError("CraftingController: Failed to instantiate preview!");
            return;
        }
        
        RemoveCollidersFromPreview(placementPreview);
        SetPreviewMaterial(placementPreview, validPlacementMaterial);
        
        // Ensure preview is active and visible
        placementPreview.SetActive(true);
        
        // Debug layer and rendering
        var renderers = placementPreview.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Debug.Log($"CraftingController: Renderer layer={LayerMask.LayerToName(renderer.gameObject.layer)}, enabled={renderer.enabled}, castShadows={renderer.shadowCastingMode}");
        }
        
        isPlacing = true;
        CraftingUIManager.Instance?.SetUIVisible(false);
        
        // Show placement instructions in HUD
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage("Left Click to Place | X to Cancel", 999f);
        }
        
        Debug.Log($"CraftingController: Started placement for {recipe.recipeName}. Preview has {renderers.Length} renderers.");
    }

    private void HandlePlacement()
    {
        if (placementPreview == null || mainCamera == null)
        {
            Debug.LogWarning($"CraftingController: Placement cancelled - Preview null: {placementPreview == null}, Camera null: {mainCamera == null}");
            CancelPlacement();
            return;
        }

        // Position preview based on camera looking direction
        Vector3 cameraForward = mainCamera.transform.position + mainCamera.transform.forward * placementMaxDistance;
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
                ConfirmPlacement(hit.point);
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
            CancelPlacement();
        }
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    private bool IsPlacementValid(RaycastHit hit)
    {
        // Add custom validation here (slope, overlaps, etc.)
        return true;
    }

    private void ConfirmPlacement(Vector3 position)
    {
        // Instantiate with parent if assigned
        GameObject spawnedObject;
        if (currentRecipe.parentTransform != null)
        {
            spawnedObject = Instantiate(currentRecipe.resultPrefab, position, Quaternion.identity, currentRecipe.parentTransform);
        }
        else
        {
            spawnedObject = Instantiate(currentRecipe.resultPrefab, position, Quaternion.identity);
        }
        
        Destroy(placementPreview);
        isPlacing = false;
        currentRecipe = null;
        
        // Show success message
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage("Pot placed successfully!", 2f);
        }
        
        // Close crafting UI and sync cursor
        CraftingUIManager.Instance?.SetUIVisible(false);
        CraftingUIManager.Instance?.RefreshUI();
        
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SetCraftOpen(false);
            KeyboardInputManager.Instance.SyncCursorState();
        }
    }

    public void CancelPlacement()
    {
        if (placementPreview != null)
            Destroy(placementPreview);

        isPlacing = false;
        currentRecipe = null;
        
        // Hide placement message
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage("Placement cancelled", 2f);
        }
        
        // Close crafting UI and sync cursor
        CraftingUIManager.Instance?.SetUIVisible(false);
        CraftingUIManager.Instance?.RefreshUI();
        
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SetCraftOpen(false);
            KeyboardInputManager.Instance.SyncCursorState();
        }
        
        Debug.Log("Placement cancelled");
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
        Debug.Log($"CraftingController: Setting preview material on {renderers.Length} renderers. Material: {(mat != null ? mat.name : "null")}");
        
        if (mat == null)
        {
            Debug.LogWarning("CraftingController: Material is null! Creating fallback material.");
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.5f, 1f, 0.5f, 0.5f); // Semi-transparent green
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
            Debug.LogWarning("CraftingController: UpdatePreviewMaterial - Material is null! Using fallback.");
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0.5f, 0.5f); // Semi-transparent red for invalid
        }
        
        var renderers = placementPreview.GetComponentsInChildren<Renderer>();
        Debug.Log($"CraftingController: Updating preview material on {renderers.Length} renderers. Position: {placementPreview.transform.position}, Material: {mat.name}");
        
        foreach (var renderer in renderers)
        {
            renderer.material = mat;
        }
    }

    public bool IsCurrentlyPlacing() => isPlacing;

    /// <summary>
    /// Draws debug gizmos for placement preview position.
    /// Orange = no raycast hit (invalid placement)
    /// Blue = raycast hit (valid placement area)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showPlacementGizmos || !isPlacing)
            return;

        // Determine which camera to use for the ray visualization
        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null)
            return;

        // Draw sphere at placement position
        Gizmos.color = lastRaycastHit ? Color.blue : new Color(1f, 0.5f, 0f); // Blue if hit, Orange if no hit
        Gizmos.DrawSphere(lastPreviewPosition, gizmoSize);
        
        // Draw a wireframe cube for better visibility
        Gizmos.DrawWireCube(lastPreviewPosition, Vector3.one * gizmoSize * 2f);
        
        // Draw a line from camera to placement point
        Gizmos.color = lastRaycastHit ? Color.blue : new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(cameraToUse.transform.position, lastPreviewPosition);
    }

}
