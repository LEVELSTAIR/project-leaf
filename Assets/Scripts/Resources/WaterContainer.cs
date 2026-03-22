using UnityEngine;

public class WaterContainer : MonoBehaviour, IInteractable
{
    [Header("Water Settings")]
    [SerializeField] private string containerName = "Water Barrel";
    [SerializeField] private int waterAmount = 100;
    [SerializeField] private int refillAmount = 10;

    [Header("Visuals")]
    [SerializeField] private GameObject waterVisual;
    [SerializeField] private Material highlightMaterial;

    private int currentWater;
    private Material originalMaterial;
    private Renderer objectRenderer;

    public string InteractionPrompt
    {
        get
        {
            if (currentWater <= 0)
                return "Water barrel is empty";
            return $"Press E to refill water (+{refillAmount})";
        }
    }

    private void Start()
    {
        currentWater = waterAmount;
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        UpdateVisual();
    }

    public void Highlight(bool active)
    {
        if (objectRenderer == null) return;

        if (active && currentWater > 0)
        {
            if (highlightMaterial != null)
                objectRenderer.material = highlightMaterial;
        }
        else
        {
            if (originalMaterial != null)
                objectRenderer.material = originalMaterial;
        }
    }

    public void Interact()
    {
        if (currentWater <= 0)
        {
            Debug.Log("The water barrel is empty!");
            return;
        }

        int amountToTake = Mathf.Min(refillAmount, currentWater);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem("Water", ItemType.Water, amountToTake);
            currentWater -= amountToTake;
            UpdateVisual();
            Debug.Log($"Took {amountToTake} water. {currentWater} remaining.");
        }
    }

    private void UpdateVisual()
    {
        if (waterVisual != null)
        {
            float waterPercent = (float)currentWater / waterAmount;
            waterVisual.transform.localScale = new Vector3(1, waterPercent, 1);
        }
    }

}
