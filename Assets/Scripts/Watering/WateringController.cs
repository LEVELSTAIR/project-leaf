using UnityEngine;
using UnityEngine.InputSystem;

public class WateringController : MonoBehaviour
{
    [Header("Watering Settings")]
    [SerializeField] private float range = 3f;
    [SerializeField] private float wateringCooldown = 0.5f;   // seconds between waterings while holding
    [SerializeField] private int waterUsagePerWatering = 1;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject waterSplashPrefab;
    [SerializeField] private Transform waterOrigin;

    private float lastWaterTime = -999f;
    private Camera playerCamera;
    private bool isWateringHeld = false;    // track if button is currently held

    private void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactableLayer == 0)
            Debug.LogWarning("WateringController: No interactable layer assigned.");
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Check if left mouse button is currently pressed
        bool isPressed = Mouse.current.leftButton.isPressed;

        // Start or stop watering held state
        if (isPressed && !isWateringHeld)
        {
            // Button just pressed – water immediately once
            isWateringHeld = true;
            TryWaterPlant();
        }
        else if (!isPressed && isWateringHeld)
        {
            // Button released
            isWateringHeld = false;
        }

        // If holding, water at cooldown rate
        if (isWateringHeld && Time.time >= lastWaterTime + wateringCooldown)
        {
            TryWaterPlant();
        }
    }

    private void TryWaterPlant()
    {
        // No need to check cooldown again here – caller handles it
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, interactableLayer))
        {
            PlantPot pot = hit.collider.GetComponent<PlantPot>();
            if (pot != null && pot.isPlanted && !pot.isReadyToHarvest)
            {
                if (InventoryManager.Instance != null &&
                    InventoryManager.Instance.HasItem("Water", ItemType.Water, waterUsagePerWatering))
                {
                    if (InventoryManager.Instance.RemoveItem("Water", ItemType.Water, waterUsagePerWatering))
                    {
                        lastWaterTime = Time.time;
                        pot.WaterPlant(waterUsagePerWatering);

                        if (waterSplashPrefab != null && waterOrigin != null)
                            Instantiate(waterSplashPrefab, waterOrigin.position, Quaternion.identity);

                        Debug.Log($"Watered plant. Used {waterUsagePerWatering} water.");
                    }
                }
                else
                {
                    // Only log once to avoid spam
                    if (Time.frameCount % 60 == 0)
                        Debug.Log("Not enough water!");
                    if (HUDManager.Instance != null && Time.frameCount % 60 == 0)
                        HUDManager.Instance.ShowMessage("Not enough water!");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.green;
        Vector3 rayOrigin = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)).origin;
        Gizmos.DrawRay(rayOrigin, playerCamera.transform.forward * range);
    }
}