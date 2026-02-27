using UnityEngine;
using UnityEngine.InputSystem;
using ProjectLeaf.Interfaces;

namespace ProjectLeaf.Player
{
    public class InteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactionDistance = 3f;
        public LayerMask interactableLayers;
        public InputActionReference interactAction;

        [Header("References")]
        public Camera playerCamera;

        private IInteractable currentInteractable;

        private void OnEnable()
        {
            if (interactAction != null)
                interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.action.Disable();
        }

        private void Update()
        {
            CheckForInteractable();

            if (interactAction != null && interactAction.action.WasPressedThisFrame())
            {
                if (currentInteractable != null)
                {
                    currentInteractable.Interact();
                }
            }
        }

        private void CheckForInteractable()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    if (currentInteractable != interactable)
                    {
                        currentInteractable = interactable;
                        PlayerUIManager.Instance?.ShowInteractionPrompt(currentInteractable.InteractionPrompt);
                        Debug.Log($"Can interact with: {currentInteractable.InteractionPrompt}");
                    }
                }
                else
                {
                    if (currentInteractable != null)
                    {
                        PlayerUIManager.Instance?.HideInteractionPrompt();
                    }
                    currentInteractable = null;
                }
            }
            else
            {
                if (currentInteractable != null)
                {
                    PlayerUIManager.Instance?.HideInteractionPrompt();
                }
                currentInteractable = null;
            }
        }
    }
}
