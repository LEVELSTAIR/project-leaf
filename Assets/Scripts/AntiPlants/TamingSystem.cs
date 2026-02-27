using UnityEngine;
using ProjectLeaf.Interfaces;

public class TamingSystem : MonoBehaviour, IInteractable
{
    [Header("Taming Settings")]
    public float maxTaming = 100f;
    public float currentTaming = 0f;
    public float tamePerInteract = 10f;
    
    private bool isInCage = false;
    private AntiPlantController controller;

    private void Awake()
    {
        controller = GetComponent<AntiPlantController>();
    }

    public string InteractionPrompt => isInCage ? "Care for Plant" : "Needs to be caged first";

    public void Interact()
    {
        if (!isInCage)
        {
            NotificationManager.Instance?.ShowNotification("It's too dangerous! Capture it in a cage first.");
            return;
        }

        if (controller != null && controller.currentState == AntiPlantController.State.Tamed)
        {
            NotificationManager.Instance?.ShowNotification("Already tamed.");
            return;
        }

        IncreaseTaming(tamePerInteract);
    }

    public void SetInCage(bool value)
    {
        isInCage = value;
    }

    private void IncreaseTaming(float amount)
    {
        currentTaming += amount;
        NotificationManager.Instance?.ShowNotification($"Taming: {currentTaming}/{maxTaming}");

        if (currentTaming >= maxTaming)
        {
            if (controller != null) controller.Tame();
        }
    }
    public void Highlight(bool active)
    {
        var highlighter = GetComponent<Highlighter>();
        if (highlighter != null) highlighter.SetHighlight(active);
    }
}
