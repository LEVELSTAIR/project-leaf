using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PlantingUIManager : MonoBehaviour
{
    public static PlantingUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement plantingPanel;
    private VisualElement seedContainer;
    private PlantPot currentPlantPot;
    private List<Button> seedButtons = new List<Button>();
    private Dictionary<Button, SeedData> buttonToSeedData = new Dictionary<Button, SeedData>();
    private bool isUIVisible = false;

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

        InitializeUI();

        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }

        // Ensure cursor is reset when disabled
        if (isUIVisible)
        {
            Time.timeScale = 1f;
            ResetCursor();
        }
    }

    private void InitializeUI()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        // Try to find existing planting panel in UXML
        plantingPanel = root.Q<VisualElement>("PlantingPanel");

        if (plantingPanel == null)
        {
            // Create planting panel programmatically
            CreatePlantingPanel();
        }
        else
        {
            // Find existing seed container
            seedContainer = plantingPanel.Q<VisualElement>("SeedContainer");
            if (seedContainer == null)
            {
                seedContainer = plantingPanel.Q<VisualElement>("SeedBox");
            }

            // Find and setup close button
            Button closeButton = plantingPanel.Q<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.clicked += HidePlantingUI;
            }
        }

        // Initially hidden
        if (plantingPanel != null)
            plantingPanel.style.display = DisplayStyle.None;
    }

    private void CreatePlantingPanel()
    {
        plantingPanel = new VisualElement();
        plantingPanel.name = "PlantingPanel";
        plantingPanel.style.position = Position.Absolute;
        plantingPanel.style.top = 0;
        plantingPanel.style.left = 0;
        plantingPanel.style.right = 0;
        plantingPanel.style.bottom = 0;
        plantingPanel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        plantingPanel.style.justifyContent = Justify.Center;
        plantingPanel.style.alignItems = Align.Center;
        plantingPanel.style.display = DisplayStyle.None;

        // Create inner container
        var innerContainer = new VisualElement();
        innerContainer.style.width = 800;
        innerContainer.style.height = 550;
        innerContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        innerContainer.style.borderTopLeftRadius = 20;
        innerContainer.style.borderTopRightRadius = 20;
        innerContainer.style.borderBottomLeftRadius = 20;
        innerContainer.style.borderBottomRightRadius = 20;
        innerContainer.style.borderTopWidth = 2;
        innerContainer.style.borderRightWidth = 2;
        innerContainer.style.borderBottomWidth = 2;
        innerContainer.style.borderLeftWidth = 2;
        innerContainer.style.borderTopColor = new Color(1, 1, 1, 0.3f);
        innerContainer.style.borderRightColor = new Color(1, 1, 1, 0.3f);
        innerContainer.style.borderBottomColor = new Color(1, 1, 1, 0.3f);
        innerContainer.style.borderLeftColor = new Color(1, 1, 1, 0.3f);
        innerContainer.style.paddingTop = 20;
        innerContainer.style.paddingBottom = 20;
        innerContainer.style.paddingLeft = 20;
        innerContainer.style.paddingRight = 20;
        innerContainer.style.flexDirection = FlexDirection.Column;

        // Title
        var title = new Label("Select a Seed to Plant");
        title.style.fontSize = 28;
        title.style.color = Color.white;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 20;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        innerContainer.Add(title);

        // ScrollView for seeds
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.style.marginBottom = 10;

        seedContainer = new VisualElement();
        seedContainer.name = "SeedContainer";
        seedContainer.style.flexDirection = FlexDirection.Row;
        seedContainer.style.flexWrap = Wrap.Wrap;
        seedContainer.style.justifyContent = Justify.Center;
        seedContainer.style.alignItems = Align.Center;
        seedContainer.style.paddingTop = 10;
        seedContainer.style.paddingBottom = 10;

        scrollView.Add(seedContainer);
        innerContainer.Add(scrollView);

        // Close button
        var closeButton = new Button();
        closeButton.name = "CloseButton";
        closeButton.text = "✕";
        closeButton.style.position = Position.Absolute;
        closeButton.style.top = 10;
        closeButton.style.right = 10;
        closeButton.style.width = 40;
        closeButton.style.height = 40;
        closeButton.style.fontSize = 20;
        closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        closeButton.style.color = Color.white;
        closeButton.style.borderTopLeftRadius = 20;
        closeButton.style.borderTopRightRadius = 20;
        closeButton.style.borderBottomLeftRadius = 20;
        closeButton.style.borderBottomRightRadius = 20;
        closeButton.clicked += HidePlantingUI;
        innerContainer.Add(closeButton);

        plantingPanel.Add(innerContainer);
        root.Add(plantingPanel);
    }

    public void ShowPlantingUI(PlantPot plantPot)
    {
        if (plantingPanel == null)
        {
            InitializeUI();
            if (plantingPanel == null)
            {
                Debug.LogError("PlantingUIManager: Failed to initialize UI!");
                return;
            }
        }

        currentPlantPot = plantPot;

        // Refresh seed buttons based on current inventory
        RefreshSeedButtons();

        // Show panel
        plantingPanel.style.display = DisplayStyle.Flex;
        isUIVisible = true;

        // Handle cursor and game state
        // Explicitly unlock cursor before pausing
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        Debug.Log($"[PlantingUI] Showing UI - Cursor visible: {UnityEngine.Cursor.visible}, LockState: {UnityEngine.Cursor.lockState}");

        // Pause game
        Time.timeScale = 0f;

        Debug.Log($"Planting UI shown with {seedButtons.Count} seed buttons");
    }

    private void RefreshSeedButtons()
    {
        if (seedContainer == null) return;

        // Clear existing buttons
        foreach (var button in seedButtons)
        {
            if (button != null && button.parent != null)
                button.RemoveFromHierarchy();
        }
        seedButtons.Clear();
        buttonToSeedData.Clear();

        // Get available seeds
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance is null!");
            ShowNoSeedsMessage("Inventory system not available!");
            return;
        }

        if (SeedManager.Instance == null)
        {
            Debug.LogError("SeedManager.Instance is null!");
            ShowNoSeedsMessage("Seed system not available!");
            return;
        }

        Dictionary<string, int> availableSeeds = InventoryManager.Instance.GetAllSeeds();
        List<SeedData> seedDataList = SeedManager.Instance.availableSeeds;

        Debug.Log($"Available seeds in inventory: {availableSeeds.Count}");

        // Create buttons for seeds in inventory
        int seedsCreated = 0;
        foreach (var seedData in seedDataList)
        {
            if (seedData == null) continue;

            int seedCount = availableSeeds.ContainsKey(seedData.seedName) ? availableSeeds[seedData.seedName] : 0;

            if (seedCount > 0)
            {
                CreateSeedButton(seedData, seedCount);
                seedsCreated++;
            }
        }

        // Show message if no seeds
        if (seedsCreated == 0)
        {
            ShowNoSeedsMessage("No seeds in your inventory!\n\nGet seeds by harvesting plants or buying from the shop.");
        }

        Debug.Log($"Created {seedsCreated} seed buttons");
    }

    private void CreateSeedButton(SeedData seedData, int seedCount)
    {
        var button = new Button();

        // Add class for styling
        button.AddToClassList("glass-button");

        // Set fixed size
        button.style.width = 140;
        button.style.height = 150;
        button.style.marginLeft = 10;
        button.style.marginRight = 10;
        button.style.marginTop = 10;
        button.style.marginBottom = 10;
        button.style.whiteSpace = WhiteSpace.Normal;
        button.style.fontSize = 14;
        button.style.paddingTop = 10;
        button.style.paddingBottom = 10;
        button.style.alignItems = Align.Center;
        button.style.justifyContent = Justify.Center;

        // Set text with seed name and count
        button.text = $"{seedData.seedName}\n({seedCount})";

        // Set icon if available
        if (seedData.seedIcon != null)
        {
            button.style.backgroundImage = new StyleBackground(seedData.seedIcon);
            button.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            button.style.unityBackgroundImageTintColor = Color.white;
        }

        // Add tooltip
        button.tooltip = $"{seedData.seedName}\nGrowth Time: {seedData.growthTime} seconds\nYield: {seedData.harvestYield} items";

        // Click handler
        SeedData capturedSeed = seedData;
        button.clicked += () => OnSeedSelected(capturedSeed);

        // Add hover effect
        button.RegisterCallback<MouseEnterEvent>(evt => {
            button.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1.05f));
        });
        button.RegisterCallback<MouseLeaveEvent>(evt => {
            button.style.scale = Scale.None();
        });

        // Add to container
        seedContainer.Add(button);
        seedButtons.Add(button);
        buttonToSeedData[button] = seedData;

        Debug.Log($"Created button for {seedData.seedName} (x{seedCount})");
    }

    private void ShowNoSeedsMessage(string message)
    {
        var noSeedsLabel = new Label(message);
        noSeedsLabel.style.color = Color.white;
        noSeedsLabel.style.fontSize = 18;
        noSeedsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        noSeedsLabel.style.whiteSpace = WhiteSpace.Normal;
        noSeedsLabel.style.width = new Length(100, LengthUnit.Percent);
        noSeedsLabel.style.paddingTop = 50;
        noSeedsLabel.style.paddingBottom = 50;
        seedContainer.Add(noSeedsLabel);
    }

    private void OnSeedSelected(SeedData seedData)
    {
        Debug.Log($"Selected seed: {seedData.seedName}");

        if (currentPlantPot != null)
        {
            currentPlantPot.PlantSeed(seedData);
            HidePlantingUI();
        }
        else
        {
            Debug.LogError("Current plant pot is null!");
            HidePlantingUI();
        }
    }

    private void OnInventoryChanged()
    {
        // Refresh buttons if UI is visible
        if (isUIVisible && plantingPanel != null && plantingPanel.style.display == DisplayStyle.Flex)
        {
            RefreshSeedButtons();
        }
    }

    public void HidePlantingUI()
    {
        if (plantingPanel != null)
            plantingPanel.style.display = DisplayStyle.None;

        currentPlantPot = null;
        isUIVisible = false;

        // Resume game
        Time.timeScale = 1f;

        // Reset cursor - following same pattern as PlayerUIManager
        ResetCursor();

        Debug.Log("Planting UI hidden");
    }

    private void ResetCursor()
    {
        // Only reset cursor if no other UI panels are open (same pattern as PlayerUIManager.ToggleInventory)
        if (KeyboardInputManager.Instance != null)
        {
            bool anyPanelOpen = KeyboardInputManager.Instance.IsAnyPanelOpen;
            Debug.Log($"[PlantingUI] ResetCursor - IsAnyPanelOpen: {anyPanelOpen}");
            
            if (!anyPanelOpen)
            {
                // No other panels open, lock the cursor
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                Debug.Log("[PlantingUI] Cursor locked (no other panels open)");
            }
            else
            {
                // Another panel is open, keep cursor visible
                Debug.Log("[PlantingUI] Cursor remains unlocked (other panels open)");
            }
        }
        else
        {
            // Fallback if KeyboardInputManager not found - lock cursor
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Debug.Log("[PlantingUI] Cursor locked (KeyboardInputManager not found)");
        }
    }

    public bool IsUIVisible => isUIVisible;

    private void Update()
    {
        // Keep cursor visible while UI is shown (in case something else tries to lock it)
        if (isUIVisible && plantingPanel != null && plantingPanel.style.display == DisplayStyle.Flex)
        {
            // Ensure cursor stays unlocked while UI is visible
            if (UnityEngine.Cursor.visible == false || UnityEngine.Cursor.lockState == CursorLockMode.Locked)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                Debug.Log("[PlantingUI] Cursor state corrected in Update");
            }
            
            // Close with Escape key when UI is visible
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HidePlantingUI();
            }
        }
    }
    
}
