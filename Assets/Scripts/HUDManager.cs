using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Mini Map")]
    public RenderTexture miniMapTexture;

    private VisualElement root;
    
    // Status Bars
    private VisualElement healthFill;
    private VisualElement staminaFill;
    
    // Clock
    private Label timeLabel;

    // Currencies (Placeholder for now)
    private Label goldLabel;
    private Label seedsLabel;
    
    // Hotbar
    private List<VisualElement> hotbarSlots = new List<VisualElement>();
    private int currentActiveSlot = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;
            InitializeUI();
        }
        else
        {
            Debug.LogError("HUDManager: No UIDocument assigned or found!");
        }
        
        // Subscribe to KeyboardInputManager events
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnHotbarSlotSelected += OnHotbarSlotChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from KeyboardInputManager events
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnHotbarSlotSelected -= OnHotbarSlotChanged;
        }
    }

    private void InitializeUI()
    {
        if (root == null) return;

        healthFill = root.Q<VisualElement>("HealthFill");
        staminaFill = root.Q<VisualElement>("StaminaFill");
        timeLabel = root.Q<Label>("TimeLabel");
        
        // Initialize Hotbar slots
        InitializeHotbar();
        
        // Mini Map connection
        VisualElement mapMask = root.Q<VisualElement>("MapMask");
        if (mapMask != null && miniMapTexture != null)
        {
            mapMask.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(miniMapTexture));
        }

        // Initial set
        UpdateHealth(1f); // 100%
        UpdateStamina(1f); // 100%
        UpdateTime("06:00");
    }
    
    private void InitializeHotbar()
    {
        hotbarSlots.Clear();
        
        VisualElement hotbarContainer = root.Q<VisualElement>("HotbarContainer");
        if (hotbarContainer == null) return;
        
        // Get all hotbar slots (children with class "hotbar-slot")
        var slots = hotbarContainer.Query<VisualElement>(className: "hotbar-slot").ToList();
        hotbarSlots.AddRange(slots);
        
        // Set initial active slot
        UpdateActiveHotbarSlot(1);
    }
    
    private void OnHotbarSlotChanged(int slotNumber)
    {
        UpdateActiveHotbarSlot(slotNumber);
    }
    
    /// <summary>
    /// Updates the visual state of hotbar slots to show which one is active.
    /// </summary>
    /// <param name="slotNumber">1-based slot number (1-5)</param>
    public void UpdateActiveHotbarSlot(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > hotbarSlots.Count) return;
        
        currentActiveSlot = slotNumber;
        
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            var slot = hotbarSlots[i];
            if (i == slotNumber - 1)
            {
                slot.AddToClassList("active-slot");
            }
            else
            {
                slot.RemoveFromClassList("active-slot");
            }
        }
    }

    public void UpdateStamina(float percentage)
    {
        if (staminaFill != null)
        {
            // Clamp between 0 and 1
            float pct = Mathf.Clamp01(percentage);
            staminaFill.style.width = Length.Percent(pct * 100f);
        }
    }

    public void UpdateHealth(float percentage)
    {
        if (healthFill != null)
        {
            float pct = Mathf.Clamp01(percentage);
            healthFill.style.width = Length.Percent(pct * 100f);
        }
    }

    public void UpdateTime(string timeString)
    {
        if (timeLabel != null)
        {
            timeLabel.text = timeString;
        }
    }
}
