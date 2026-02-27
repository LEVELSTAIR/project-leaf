using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3.0f;
    public LayerMask interactionLayer;

    private Camera playerCamera;
    private IInteractable currentInteractable;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            // Fallback if main camera isn't tagged, try to find it on this object or children
            playerCamera = GetComponentInChildren<Camera>();
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
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    // New object found, unhighlight old one
                    if (currentInteractable != null)
                    {
                        currentInteractable.Highlight(false);
                    }

                    // Highlight new one
                    currentInteractable = interactable;
                    currentInteractable.Highlight(true);
                    
                    // Show UI Prompt
                    if (PlayerUIManager.Instance != null)
                    {
                        PlayerUIManager.Instance.ShowInteractionPrompt(currentInteractable.InteractionPrompt);
                    }
                }
                return; // Found something, stop processing
            }
        }

        // Nothing found or raycast missed
        if (currentInteractable != null)
        {
            currentInteractable.Highlight(false);
            
            // Hide UI Prompt
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.HideInteractionPrompt();
            }
            
            currentInteractable = null;
        }
    }

    private void HandleInput()
    {
        // Check for E key (Use Item) or F key (Interact/KeyboardInputManager default)
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        
        if (currentInteractable != null && ePressed)
        {
            currentInteractable.Interact();
        }
    }
}
