using UnityEngine;

public class Well : MonoBehaviour, IInteractable
{
    [Header("Water Settings")]
    [SerializeField] private string wellName = "Well";
    [SerializeField] private int waterPerCollect = 10;
    [SerializeField] private bool infiniteSupply = true;
    [SerializeField] private int totalWaterAmount = 100;

    [Header("Refill Settings")]
    [SerializeField] private float refillTime = 60f; // Time in seconds for well to fully refill
    [SerializeField] private bool showRefillTimerInPrompt = true;

    [Header("Visuals")]
    [SerializeField] private GameObject waterVisual;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private bool hideWaterWhenEmpty = true;
    [SerializeField] private bool preserveXZScale = true; // Keep X and Z scale constant, only change Y

    private int currentWaterAmount;
    private Renderer objectRenderer;
    private Material originalMaterial;
    private float lastCollectTime;
    private bool isDepleted = false;
    private Vector3 originalWaterScale; // Store original scale on start

    public int WaterPerCollect => waterPerCollect;
    public bool IsEmpty => !infiniteSupply && currentWaterAmount <= 0;

    // IInteractable – prompt is read by your interaction manager
    public string InteractionPrompt
    {
        get
        {
            if (IsEmpty)
            {
                if (showRefillTimerInPrompt)
                {
                    float timeRemaining = refillTime - (Time.time - lastCollectTime);
                    if (timeRemaining > 0)
                    {
                        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                        return $"Well is dry. Refills in {minutes:00}:{seconds:00}";
                    }
                }
                return "Well is dry";
            }
            return $"Hold E to collect water (+{waterPerCollect})";
        }
    }

    private void Start()
    {
        currentWaterAmount = totalWaterAmount;
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
            originalMaterial = objectRenderer.material;

        // Store original water visual scale for reference
        if (waterVisual != null)
        {
            originalWaterScale = waterVisual.transform.localScale;
            Debug.Log($"[Well] Water visual original scale: X={originalWaterScale.x}, Y={originalWaterScale.y}, Z={originalWaterScale.z}");
        }

        UpdateVisual();
    }

    private void Update()
    {
        // Check if well should refill
        if (!infiniteSupply && isDepleted && Time.time >= lastCollectTime + refillTime)
        {
            RefillWell();
        }
    }

    // Called by the WellCollectorController
    public int CollectWater()
    {
        if (IsEmpty) return 0;

        int amountToGive = waterPerCollect;
        if (!infiniteSupply && currentWaterAmount < waterPerCollect)
            amountToGive = currentWaterAmount;

        if (!infiniteSupply)
        {
            currentWaterAmount -= amountToGive;

            // Mark well as depleted and start refill timer
            if (currentWaterAmount <= 0)
            {
                isDepleted = true;
                lastCollectTime = Time.time;

                // Hide water visual when depleted
                if (hideWaterWhenEmpty && waterVisual != null)
                {
                    waterVisual.SetActive(false);
                }
            }

            UpdateVisual();
        }

        return amountToGive;
    }

    private void RefillWell()
    {
        currentWaterAmount = totalWaterAmount;
        isDepleted = false;

        // Show water visual when refilled
        if (hideWaterWhenEmpty && waterVisual != null)
        {
            waterVisual.SetActive(true);
        }

        UpdateVisual();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage($"{wellName} has refilled!", 2f);
        }

        Debug.Log($"{wellName} has refilled!");
    }

    // IInteractable – intentionally empty (collection is handled by controller)
    public void Interact() { }

    public void Highlight(bool active)
    {
        if (objectRenderer == null) return;
        if (active && !IsEmpty && highlightMaterial != null)
            objectRenderer.material = highlightMaterial;
        else if (originalMaterial != null)
            objectRenderer.material = originalMaterial;
    }

    private void UpdateVisual()
    {
        if (waterVisual != null && totalWaterAmount > 0)
        {
            float percent = (float)currentWaterAmount / totalWaterAmount;

            if (preserveXZScale)
            {
                // Only scale Y axis, keep X and Z at original values
                waterVisual.transform.localScale = new Vector3(originalWaterScale.x, originalWaterScale.y * percent, originalWaterScale.z);
            }
            else
            {
                // Original behavior: scale all axes
                waterVisual.transform.localScale = new Vector3(1, percent, 1);
            }

            Debug.Log($"[Well] Water visual scaled to {percent * 100f:F1}% - Scale: X={waterVisual.transform.localScale.x}, Y={waterVisual.transform.localScale.y}, Z={waterVisual.transform.localScale.z}");
        }
    }
}
