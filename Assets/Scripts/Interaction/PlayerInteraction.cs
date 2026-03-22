using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3.0f;
    public LayerMask interactionLayer;

    [Header("Debug Settings")]
    public bool showDebugRay = true;
    public bool showDebugMessages = true;
    public Color rayColor = Color.green;
    public Color hitColor = Color.red;
    public float rayDuration = 0f;

    private Camera playerCamera;
    private IInteractable currentInteractable;
    private RaycastHit lastHit;
    private GameObject lastHitObject;

    private void Start()
    {
        playerCamera = GetComponent<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction: No Camera component found on this GameObject!");
        }

        if (HUDManager.Instance == null)
        {
            Debug.LogWarning("PlayerInteraction: HUDManager.Instance not found.");
        }
    }

    private void Update()
    {
        HandleRaycast();
        HandleInput();
    }

    private void HandleRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out lastHit, interactionDistance, interactionLayer))
        {
            IInteractable interactable = lastHit.collider.GetComponent<IInteractable>();

            if (showDebugMessages)
            {
                Debug.Log($"Ray hit: {lastHit.collider.gameObject.name}");
            }

            if (interactable != null)
            {
                if (showDebugMessages && lastHitObject != lastHit.collider.gameObject)
                {
                    Debug.Log($"<color=green>INTERACTABLE: {lastHit.collider.gameObject.name}</color>");
                }

                if (currentInteractable != interactable)
                {
                    if (currentInteractable != null)
                    {
                        currentInteractable.Highlight(false);
                    }

                    currentInteractable = interactable;
                    currentInteractable.Highlight(true);

                    if (HUDManager.Instance != null)
                    {
                        HUDManager.Instance.ShowInteractionPrompt(currentInteractable.InteractionPrompt);
                    }
                }

                lastHitObject = lastHit.collider.gameObject;

                if (showDebugRay)
                {
                    Debug.DrawRay(ray.origin, ray.direction * lastHit.distance, hitColor, rayDuration);
                    Debug.DrawLine(lastHit.point, lastHit.point + lastHit.normal * 0.5f, Color.blue, rayDuration);
                }

                return;
            }
            else
            {
                if (showDebugMessages && lastHitObject != lastHit.collider.gameObject)
                {
                    Debug.Log($"<color=orange>Hit: {lastHit.collider.gameObject.name} (not interactable)</color>");
                }
                lastHitObject = lastHit.collider.gameObject;

                if (currentInteractable != null)
                {
                    if (HUDManager.Instance != null)
                    {
                        HUDManager.Instance.HideInteractionPrompt();
                    }
                    currentInteractable.Highlight(false);
                    currentInteractable = null;
                }
            }
        }
        else
        {
            if (showDebugMessages && lastHitObject != null)
            {
                Debug.Log("Raycast missed");
                lastHitObject = null;
            }

            if (currentInteractable != null)
            {
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.HideInteractionPrompt();
                }
                currentInteractable.Highlight(false);
                currentInteractable = null;
            }
        }

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, rayColor, rayDuration);
        }
    }

    private void HandleInput()
    {
        bool fPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

        if (currentInteractable != null && fPressed)
        {
            if (showDebugMessages)
            {
                Debug.Log($"<color=magenta>INTERACTED with {lastHitObject?.name}</color>");
            }
            currentInteractable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugRay || playerCamera == null) return;

        Gizmos.color = Color.yellow;
        Vector3 rayEnd = playerCamera.transform.position + playerCamera.transform.forward * interactionDistance;
        Gizmos.DrawWireSphere(rayEnd, 0.1f);

        Gizmos.color = rayColor;
        Gizmos.DrawLine(playerCamera.transform.position, rayEnd);
    }

}
