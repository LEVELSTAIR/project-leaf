using UnityEngine;
using UnityEngine.InputSystem;

public class WaterCollectorController : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float range = 3f;
    [SerializeField] private float collectionCooldown = 0.5f;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask wellLayer;   // assign "Interactable" layer (same as your IInteractable objects)

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectSplashPrefab;
    [SerializeField] private Transform collectOrigin;

    private float lastCollectTime = -999f;
    private Camera playerCamera;
    private Well currentWell = null;
    private bool isHolding = false;

    private void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        // 1. Find the well the player is looking at
        UpdateCurrentWell();

        // 2. Handle hold‑to‑collect
        if (currentWell != null && !currentWell.IsEmpty)
        {
            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            if (ePressed)
            {
                if (!isHolding)
                {
                    isHolding = true;
                    TryCollect();
                }
                else if (Time.time >= lastCollectTime + collectionCooldown)
                {
                    TryCollect();
                }
            }
            else
            {
                isHolding = false;
            }
        }
        else
        {
            isHolding = false;
        }
    }

    private void UpdateCurrentWell()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, wellLayer))
        {
            Well well = hit.collider.GetComponent<Well>();
            if (well != currentWell)
            {
                currentWell = well;
            }
        }
        else
        {
            currentWell = null;
        }
    }

    private void TryCollect()
    {
        if (currentWell == null || currentWell.IsEmpty) return;

        int collected = currentWell.CollectWater();
        if (collected > 0 && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem("Water", ItemType.Water, collected);
            lastCollectTime = Time.time;

            if (collectSplashPrefab != null && collectOrigin != null)
                Instantiate(collectSplashPrefab, collectOrigin.position, Quaternion.identity);

            if (HUDManager.Instance != null)
                HUDManager.Instance.ShowMessage($"Collected {collected} water", 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.blue;
        Vector3 rayOrigin = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)).origin;
        Gizmos.DrawRay(rayOrigin, playerCamera.transform.forward * range);
    }
}
