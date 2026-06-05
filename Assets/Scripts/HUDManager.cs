using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Mini Map")]
    public RenderTexture miniMapTexture;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.6f);

    private VisualElement root;
    private VisualElement damageOverlay;
    private Coroutine flashCoroutine;

    // Status Values
    private Label healthValueLabel;
    private Label staminaValueLabel;
    private Label oxygenValueLabel;

    // Clock
    private Label timeLabel;

    // Currencies (Placeholder for now)
    private Label goldLabel;
    private Label seedsLabel;

    // Hotbar
    private List<VisualElement> hotbarSlots = new List<VisualElement>();
    private int currentActiveSlot = 1;

    // Interaction Prompt
    private Label interactionPromptLabel;
    private VisualElement interactionContainer;

    // Inventory Display
    private Label goldDisplayLabel;
    private Label waterDisplayLabel;
    private Label clayDisplayLabel;
    private VisualElement seedsContainer;

    // Message System
    private Label messageLabel;
    private Coroutine messageCoroutine;

    // Pause Button
    private Button pauseButton;

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
            CreateDamageOverlay();   // NEW: creates the red flash overlay
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

        // Stop any running coroutines
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
    }

    private void InitializeUI()
    {
        if (root == null) return;

        // Find existing UI elements
        healthValueLabel = root.Q<Label>("HealthValue");
        staminaValueLabel = root.Q<Label>("StaminaValue");
        oxygenValueLabel = root.Q<Label>("OxygenValue");
        timeLabel = root.Q<Label>("TimeLabel");

        // Initialize Hotbar slots
        InitializeHotbar();

        // Mini Map connection
        VisualElement mapMask = root.Q<VisualElement>("MapMask");
        if (mapMask != null && miniMapTexture != null)
        {
            mapMask.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(miniMapTexture));
        }

        // Initialize UI components
        InitializeInteractionPrompt();
        InitializeInventoryUI();
        InitializeMessageUI();
        InitializePauseButton();

        // Initial values
        UpdateHealth(1f);
        UpdateStamina(1f);
        UpdateOxygen(1f);
        UpdateTime("06:00");

        // Hide prompts initially
        HideInteractionPrompt();
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

    private void InitializeInteractionPrompt()
    {
        // Try to find existing interaction prompt elements
        interactionContainer = root.Q<VisualElement>("InteractionContainer");
        interactionPromptLabel = root.Q<Label>("InteractionPrompt");

        // If not found, create default prompt
        if (interactionContainer == null && interactionPromptLabel == null)
        {
            CreateDefaultInteractionPrompt();
        }
    }

    private void CreateDefaultInteractionPrompt()
    {
        // Create container
        interactionContainer = new VisualElement();
        interactionContainer.name = "InteractionContainer";
        interactionContainer.style.position = Position.Absolute;
        interactionContainer.style.bottom = 100;
        interactionContainer.style.left = 0;
        interactionContainer.style.right = 0;
        interactionContainer.style.alignItems = Align.Center;
        interactionContainer.style.display = DisplayStyle.None;

        // Create background
        var background = new VisualElement();
        background.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        background.style.paddingTop = 12;
        background.style.paddingBottom = 12;
        background.style.paddingLeft = 24;
        background.style.paddingRight = 24;
        background.style.borderTopLeftRadius = 8;
        background.style.borderTopRightRadius = 8;
        background.style.borderBottomLeftRadius = 8;
        background.style.borderBottomRightRadius = 8;

        // Create label
        interactionPromptLabel = new Label();
        interactionPromptLabel.name = "InteractionPrompt";
        interactionPromptLabel.text = "Press E to interact";
        interactionPromptLabel.style.color = Color.white;
        interactionPromptLabel.style.fontSize = 18;
        interactionPromptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        interactionPromptLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        // Assemble
        background.Add(interactionPromptLabel);
        interactionContainer.Add(background);
        root.Add(interactionContainer);
    }

    private void InitializeInventoryUI()
    {
        if (root == null) return;

        // Try to find existing UI elements (bottom amount labels)
        goldDisplayLabel = root.Q<Label>("GoldValue");
        waterDisplayLabel = root.Q<Label>("WaterValue");
        clayDisplayLabel = root.Q<Label>("ClayValue");
        seedsContainer = root.Q<VisualElement>("SeedsContainer");

        // Create default gold display if not found
        if (goldDisplayLabel == null)
        {
            goldDisplayLabel = new Label("0");
            goldDisplayLabel.name = "GoldValue";
            goldDisplayLabel.style.position = Position.Absolute;
            goldDisplayLabel.style.top = 10;
            goldDisplayLabel.style.right = 10;
            goldDisplayLabel.style.color = Color.yellow;
            goldDisplayLabel.style.fontSize = 16;
            goldDisplayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            goldDisplayLabel.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            goldDisplayLabel.style.paddingTop = 5;
            goldDisplayLabel.style.paddingBottom = 5;
            goldDisplayLabel.style.paddingLeft = 10;
            goldDisplayLabel.style.paddingRight = 10;
            goldDisplayLabel.style.borderTopLeftRadius = 5;
            goldDisplayLabel.style.borderTopRightRadius = 5;
            goldDisplayLabel.style.borderBottomLeftRadius = 5;
            goldDisplayLabel.style.borderBottomRightRadius = 5;
            root.Add(goldDisplayLabel);
        }

        // Create default water display if not found
        if (waterDisplayLabel == null)
        {
            waterDisplayLabel = new Label("0");
            waterDisplayLabel.name = "WaterValue";
            waterDisplayLabel.style.position = Position.Absolute;
            waterDisplayLabel.style.top = 45;
            waterDisplayLabel.style.right = 10;
            waterDisplayLabel.style.color = Color.cyan;
            waterDisplayLabel.style.fontSize = 16;
            waterDisplayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            waterDisplayLabel.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            waterDisplayLabel.style.paddingTop = 5;
            waterDisplayLabel.style.paddingBottom = 5;
            waterDisplayLabel.style.paddingLeft = 10;
            waterDisplayLabel.style.paddingRight = 10;
            waterDisplayLabel.style.borderTopLeftRadius = 5;
            waterDisplayLabel.style.borderTopRightRadius = 5;
            waterDisplayLabel.style.borderBottomLeftRadius = 5;
            waterDisplayLabel.style.borderBottomRightRadius = 5;
            root.Add(waterDisplayLabel);
        }

        // Create default seeds container if not found
        if (seedsContainer == null)
        {
            seedsContainer = new VisualElement();
            seedsContainer.name = "SeedsContainer";
            seedsContainer.style.position = Position.Absolute;
            seedsContainer.style.top = 115;
            seedsContainer.style.right = 10;
            seedsContainer.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            seedsContainer.style.paddingTop = 12;
            seedsContainer.style.paddingBottom = 12;
            seedsContainer.style.paddingLeft = 16;
            seedsContainer.style.paddingRight = 16;
            seedsContainer.style.borderTopLeftRadius = 5;
            seedsContainer.style.borderTopRightRadius = 5;
            seedsContainer.style.borderBottomLeftRadius = 5;
            seedsContainer.style.borderBottomRightRadius = 5;
            seedsContainer.style.minWidth = 180;
            root.Add(seedsContainer);
        }

        // Initialize seeds display
        UpdateSeeds(new Dictionary<string, int>());
    }

    private void InitializeMessageUI()
    {
        if (root == null) return;

        messageLabel = root.Q<Label>("MessageLabel");

        if (messageLabel == null)
        {
            messageLabel = new Label();
            messageLabel.name = "MessageLabel";
            messageLabel.style.position = Position.Absolute;
            messageLabel.style.bottom = 150;
            messageLabel.style.left = 0;
            messageLabel.style.right = 0;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.color = Color.white;
            messageLabel.style.fontSize = 18;
            messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            messageLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            messageLabel.style.paddingTop = 10;
            messageLabel.style.paddingBottom = 10;
            messageLabel.style.paddingLeft = 20;
            messageLabel.style.paddingRight = 20;
            messageLabel.style.borderTopLeftRadius = 5;
            messageLabel.style.borderTopRightRadius = 5;
            messageLabel.style.borderBottomLeftRadius = 5;
            messageLabel.style.borderBottomRightRadius = 5;
            messageLabel.style.display = DisplayStyle.None;
            root.Add(messageLabel);
        }
    }

    private void InitializePauseButton()
    {
        pauseButton = root.Q<Button>("PauseButton");
        if (pauseButton != null)
            pauseButton.clicked += () => KeyboardInputManager.Instance?.OpenEscapeMenu();
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
            if (slot == null) continue;

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
        if (staminaValueLabel != null)
        {
            float pct = Mathf.Clamp01(percentage);
            staminaValueLabel.text = $"{Mathf.RoundToInt(pct * 100f)}%";
        }
    }

    public void UpdateHealth(float percentage)
    {
        if (healthValueLabel != null)
        {
            float pct = Mathf.Clamp01(percentage);
            healthValueLabel.text = $"{Mathf.RoundToInt(pct * 100f)}%";
        }
    }

    public void UpdateTime(string timeString)
    {
        if (timeLabel != null)
        {
            timeLabel.text = timeString;
        }
    }

    public void UpdateOxygen(float percentage)
    {
        if (oxygenValueLabel != null)
        {
            float pct = Mathf.Clamp01(percentage);
            oxygenValueLabel.text = $"{Mathf.RoundToInt(pct * 100f)}%";
        }
    }

    /// <summary>
    /// Show interaction prompt with custom message
    /// </summary>
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptLabel != null)
        {
            interactionPromptLabel.text = message;
            interactionPromptLabel.style.display = DisplayStyle.Flex;
        }

        if (interactionContainer != null)
        {
            interactionContainer.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Hide interaction prompt
    /// </summary>
    public void HideInteractionPrompt()
    {
        if (interactionPromptLabel != null)
        {
            interactionPromptLabel.style.display = DisplayStyle.None;
        }

        if (interactionContainer != null)
        {
            interactionContainer.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Update prompt text without changing visibility
    /// </summary>
    public void UpdateInteractionPrompt(string newMessage)
    {
        if (interactionPromptLabel != null)
        {
            interactionPromptLabel.text = newMessage;
        }
    }

    /// <summary>
    /// Update gold display
    /// </summary>
    public void UpdateGold(int goldAmount)
    {
        if (goldDisplayLabel != null)
        {
            goldDisplayLabel.text = goldAmount.ToString();
        }
    }

    /// <summary>
    /// Update water display
    /// </summary>
    public void UpdateWater(int waterAmount)
    {
        if (waterDisplayLabel != null)
        {
            waterDisplayLabel.text = waterAmount.ToString();
        }
    }

    /// <summary>
    /// Update clay display
    /// </summary>
    public void UpdateClay(int clayAmount)
    {
        if (clayDisplayLabel != null)
        {
            clayDisplayLabel.text = clayAmount.ToString();
        }
    }

    /// <summary>
    /// Update seeds display
    /// </summary>
    public void UpdateSeeds(Dictionary<string, int> seeds)
    {
        if (seedsContainer == null) return;

        seedsContainer.Clear();

        // Add title
        var title = new Label("🌱 Seeds");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 8;
        title.style.color = new Color(0.8f, 0.8f, 0.5f);
        title.style.fontSize = 18;
        seedsContainer.Add(title);

        if (seeds == null || seeds.Count == 0)
        {
            var emptyLabel = new Label("No seeds");
            emptyLabel.style.color = Color.gray;
            emptyLabel.style.marginTop = 4;
            emptyLabel.style.fontSize = 16;
            seedsContainer.Add(emptyLabel);
        }
        else
        {
            // Add separator line
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            separator.style.marginTop = 2;
            separator.style.marginBottom = 6;
            seedsContainer.Add(separator);

            foreach (var seed in seeds)
            {
                var seedElement = new VisualElement();
                seedElement.style.flexDirection = FlexDirection.Row;
                seedElement.style.justifyContent = Justify.SpaceBetween;
                seedElement.style.marginTop = 3;
                seedElement.style.marginBottom = 3;

                var seedNameLabel = new Label(seed.Key);
                seedNameLabel.style.color = Color.white;
                seedNameLabel.style.fontSize = 16;

                var seedAmountLabel = new Label($"x{seed.Value}");
                seedAmountLabel.style.color = new Color(0.8f, 0.8f, 0.5f);
                seedAmountLabel.style.fontSize = 16;
                seedAmountLabel.style.marginLeft = 10;

                seedElement.Add(seedNameLabel);
                seedElement.Add(seedAmountLabel);

                seedsContainer.Add(seedElement);
            }
        }
    }

    /// <summary>
    /// Show a temporary message on screen
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        if (messageLabel == null) return;

        // Stop any existing message coroutine
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        // Update message text and show
        messageLabel.text = message;
        messageLabel.style.display = DisplayStyle.Flex;

        // Start coroutine to hide after duration
        messageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
    }

    private IEnumerator HideMessageAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (messageLabel != null)
        {
            // Fade out effect (optional)
            float fadeTime = 0.3f;
            float elapsed = 0;
            Color startColor = messageLabel.style.color.value;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / fadeTime);
                messageLabel.style.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            messageLabel.style.display = DisplayStyle.None;
            messageLabel.style.color = Color.white; // Reset color for next message
        }

        messageCoroutine = null;
    }

    /// <summary>
    /// Update all inventory displays at once
    /// </summary>
    public void UpdateAllInventory()
    {
        if (InventoryManager.Instance != null)
        {
            UpdateGold(InventoryManager.Instance.GetTotalGold());
            UpdateWater(InventoryManager.Instance.GetTotalWater());
            UpdateClay(InventoryManager.Instance.GetTotalClay());
            UpdateSeeds(InventoryManager.Instance.GetAllSeeds());
        }
    }

    /// <summary>
    /// Get the current interaction prompt text
    /// </summary>
    public string GetCurrentPromptText()
    {
        if (interactionPromptLabel != null)
        {
            return interactionPromptLabel.text;
        }
        return string.Empty;
    }

    /// <summary>
    /// Check if interaction prompt is visible
    /// </summary>
    public bool IsInteractionPromptVisible()
    {
        if (interactionContainer != null)
        {
            return interactionContainer.style.display == DisplayStyle.Flex;
        }
        return false;
    }

    // ---------- NEW: Damage Flash Overlay ----------
    private void CreateDamageOverlay()
    {
        if (root == null) return;

        damageOverlay = new VisualElement();
        damageOverlay.name = "DamageOverlay";
        damageOverlay.style.position = Position.Absolute;
        damageOverlay.style.top = 0;
        damageOverlay.style.left = 0;
        damageOverlay.style.right = 0;
        damageOverlay.style.bottom = 0;
        damageOverlay.style.backgroundColor = flashColor;
        damageOverlay.style.opacity = 0;
        damageOverlay.pickingMode = PickingMode.Ignore;
        damageOverlay.style.display = DisplayStyle.None;
        root.Add(damageOverlay);
    }

    /// <summary>
    /// Show a red flash overlay (called when player takes damage).
    /// </summary>
    public void ShowDamageFlash()
    {
        if (damageOverlay == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        damageOverlay.style.display = DisplayStyle.Flex;
        damageOverlay.style.opacity = flashColor.a;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            float newAlpha = Mathf.Lerp(flashColor.a, 0f, t);
            damageOverlay.style.opacity = newAlpha;
            yield return null;
        }

        damageOverlay.style.opacity = 0;
        damageOverlay.style.display = DisplayStyle.None;
        flashCoroutine = null;
    }
}