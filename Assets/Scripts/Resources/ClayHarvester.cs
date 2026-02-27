using UnityEngine;
using ProjectLeaf.Interfaces;

public class ClayHarvester : MonoBehaviour, IInteractable
{
    [Header("Harvest Settings")]
    public string resourceName = "Clay Deposit";
    public float harvestTime = 1.5f;
    public int amountPerHarvest = 2;
    public int maxHarvests = 3;

    private int currentHarvests = 0;

    public string InteractionPrompt => currentHarvests >= maxHarvests ? "Depleted" : $"Harvest {resourceName}";

    public void Interact()
    {
        if (currentHarvests >= maxHarvests)
        {
            NotificationManager.Instance?.ShowNotification("This deposit is depleted.");
            return;
        }

        StartCoroutine(HarvestProcess());
    }

    private System.Collections.IEnumerator HarvestProcess()
    {
        NotificationManager.Instance?.ShowNotification("Digging for clay...");
        
        yield return new WaitForSeconds(harvestTime);

        currentHarvests++;
        NotificationManager.Instance?.ShowNotification($"Collected {amountPerHarvest} Clay!");

        // Add to inventory (Placeholder)
        // InventoryManager.Instance.AddItem("Clay", amountPerHarvest);

        if (currentHarvests >= maxHarvests)
        {
            // Visual feedback for depletion
            GetComponent<Renderer>().material.color = Color.gray;
        }
    }
    public void Highlight(bool active)
    {
        var highlighter = GetComponent<Highlighter>();
        if (highlighter != null) highlighter.SetHighlight(active);
    }
}
