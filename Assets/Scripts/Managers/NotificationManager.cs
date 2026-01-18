using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI Settings")]
    public UIDocument hudDocument;
    public string notificationContainerName = "NotificationContainer";
    public VisualTreeAsset notificationTemplate;
    public float defaultDuration = 3f;

    private VisualElement container;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (hudDocument != null)
        {
            var root = hudDocument.rootVisualElement;
            container = root.Q<VisualElement>(notificationContainerName);
        }
    }

    public void ShowNotification(string message, float duration = -1)
    {
        if (container == null || notificationTemplate == null)
        {
            Debug.Log($"[Notification] {message}");
            return;
        }

        if (duration < 0) duration = defaultDuration;

        // Instantiate template
        TemplateContainer toast = notificationTemplate.Instantiate();
        Label label = toast.Q<Label>("MessageLabel"); // Assuming Label name
        if (label != null) label.text = message;

        container.Add(toast);

        // Animation / Removal
        StartCoroutine(RemoveNotificationRoutine(toast, duration));
    }

    private System.Collections.IEnumerator RemoveNotificationRoutine(VisualElement element, float duration)
    {
        // Simple fade out could be added here via USS classes transition
        yield return new WaitForSeconds(duration);
        
        if (container.Contains(element))
        {
            container.Remove(element);
        }
    }
}
