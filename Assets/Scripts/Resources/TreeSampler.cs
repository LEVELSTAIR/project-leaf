using UnityEngine;
using ProjectLeaf.Interfaces;

public class TreeSampler : MonoBehaviour, IInteractable
{
    [Header("Sampling Settings")]
    public string treeName = "Ancient Oak";
    public float sampleDuration = 2.0f;
    [Range(0f, 1f)] public float seedDropChance = 0.5f;
    public GameObject seedPrefab;
    
    private bool hasBeenSampled = false;

    // Interface Implementation
    public string InteractionPrompt => hasBeenSampled ? "Already Sampled" : $"Sample {treeName}";

    public void Interact()
    {
        if (hasBeenSampled)
        {
            NotificationManager.Instance?.ShowNotification("This tree has already been sampled specifically.");
            return;
        }

        StartCoroutine(SamplingProcess());
    }

    private System.Collections.IEnumerator SamplingProcess()
    {
        // TODO: Play animation / Sound
        NotificationManager.Instance?.ShowNotification($"Taking sample from {treeName}...");
        
        yield return new WaitForSeconds(sampleDuration);

        hasBeenSampled = true;
        NotificationManager.Instance?.ShowNotification("Sample collected!");
        
        // Add to inventory (Placeholder call)
        // InventoryManager.Instance.AddItem("TreeSample", 1);

        CheckForSeedDrop();
    }

    private void CheckForSeedDrop()
    {
        if (Random.value <= seedDropChance && seedPrefab != null)
        {
            Instantiate(seedPrefab, transform.position + Vector3.up + transform.forward, Quaternion.identity);
            NotificationManager.Instance?.ShowNotification("You found a seed!");
        }
    }
    public void Highlight(bool active)
    {
        var highlighter = GetComponent<Highlighter>();
        if (highlighter != null) highlighter.SetHighlight(active);
    }
}
