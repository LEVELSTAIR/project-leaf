using UnityEngine;

[RequireComponent(typeof(Highlighter))]
public class ResourceNode : MonoBehaviour, IInteractable
{
    [Header("Resource Settings")]
    public string resourceName = "Wood";
    public string notificationMessage = "Collected Wood +1";
    
    public string InteractionPrompt => $"Harvest {resourceName}";
    
    // We can add more logic here later (e.g. adding to inventory)

    private Highlighter highlighter;

    private void Awake()
    {
        highlighter = GetComponent<Highlighter>();
    }

    public void Highlight(bool active)
    {
        if (highlighter != null)
        {
            highlighter.SetHighlight(active);
        }
    }

    public void Interact()
    {
        // 1. Show Feedback
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification(notificationMessage);
        }
        else
        {
            Debug.Log($"[Mock Notification] {notificationMessage}");
        }

        // 2. Disable object to simulate "Collection"
        // In the future, this would add to inventory and perhaps play a sound/particle effect
        gameObject.SetActive(false);
    }
}
