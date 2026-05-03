using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class FlowerSeedPlantingUIManager : MonoBehaviour
{
    public static FlowerSeedPlantingUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Flower Seeds Reference")]
    public List<FlowerSeedData> availableFlowerSeeds = new List<FlowerSeedData>();

    // UI Element references
    private VisualElement root;
    private VisualElement plantingPanel;
    private VisualElement flowerSeedContainer;
    private Button closeButton;

    // Keep track of flower seed buttons
    private Dictionary<FlowerSeedData, Button> flowerSeedButtons = new Dictionary<FlowerSeedData, Button>();

    private SoilController currentSoil;
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
        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
        }
    }

    private void Start()
    {
        InitializeUI();
        BuildFlowerSeedList();
        SetUIVisible(false);
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("FlowerSeedPlantingUIManager: UIDocument not assigned!");
            return;
        }

        root = uiDocument.rootVisualElement;
        plantingPanel = root.Q<VisualElement>("planting-panel");
        flowerSeedContainer = root.Q<VisualElement>("flower-seed-list");
        closeButton = root.Q<Button>("close-button");

        if (closeButton != null)
        {
            closeButton.clicked += OnCloseButtonClicked;
        }
    }

    private void OnCloseButtonClicked()
    {
        SetUIVisible(false);

        if (FlowerSeedPlantingController.Instance != null)
        {
            FlowerSeedPlantingController.Instance.CancelPlanting();
        }

        // Sync cursor state with KeyboardInputManager
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SyncCursorState();
        }
    }

    private void BuildFlowerSeedList()
    {
        if (flowerSeedContainer == null) return;

        flowerSeedContainer.Clear();
        flowerSeedButtons.Clear();

        foreach (var flowerSeed in availableFlowerSeeds)
        {
            var flowerEntry = CreateFlowerSeedEntry(flowerSeed);
            flowerSeedContainer.Add(flowerEntry);
        }
    }

    private VisualElement CreateFlowerSeedEntry(FlowerSeedData flowerSeed)
    {
        var entry = new VisualElement();
        entry.AddToClassList("flower-seed-entry");

        var iconContainer = new VisualElement();
        iconContainer.AddToClassList("flower-icon");
        if (flowerSeed.seedIcon != null)
        {
            iconContainer.style.backgroundImage = new StyleBackground(flowerSeed.seedIcon);
        }
        entry.Add(iconContainer);

        var nameLabel = new Label(flowerSeed.seedName);
        nameLabel.AddToClassList("flower-name");
        entry.Add(nameLabel);

        var amountLabel = new Label(GetAmountText(flowerSeed));
        amountLabel.name = "amount";
        amountLabel.AddToClassList("flower-amount");
        entry.Add(amountLabel);

        var plantButton = new Button(() => OnPlantClicked(flowerSeed));
        plantButton.name = "plant-button";
        plantButton.text = "Plant";
        plantButton.AddToClassList("plant-button");
        entry.Add(plantButton);

        flowerSeedButtons[flowerSeed] = plantButton;

        return entry;
    }

    private string GetAmountText(FlowerSeedData flowerSeed)
    {
        if (InventoryManager.Instance == null)
            return "0";

        int current = InventoryManager.Instance.GetItemAmount(flowerSeed.seedName, ItemType.FlowerSeeds);
        return $"Have: {current}";
    }

    private void OnPlantClicked(FlowerSeedData flowerSeed)
    {
        if (currentSoil == null)
        {
            Debug.LogWarning("FlowerSeedPlantingUIManager: No soil selected!");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("FlowerSeedPlantingUIManager: InventoryManager not found!");
            return;
        }

        // Check if player has the flower seed
        if (!InventoryManager.Instance.HasItem(flowerSeed.seedName, ItemType.FlowerSeeds, 1))
        {
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage($"Not enough {flowerSeed.seedName}!", 2f);
            }
            return;
        }

        // Start planting mode
        if (FlowerSeedPlantingController.Instance != null)
        {
            FlowerSeedPlantingController.Instance.StartPlantingMode(flowerSeed, currentSoil, 1);
        }
    }

    public void ShowUI(SoilController soil)
    {
        currentSoil = soil;
        SetUIVisible(true);
    }

    public void RefreshUI()
    {
        if (flowerSeedContainer == null) return;

        // Update amounts and button interactability
        foreach (var kvp in flowerSeedButtons)
        {
            var flowerSeed = kvp.Key;
            var button = kvp.Value;

            var entry = button.parent;
            var amountLabel = entry.Q<Label>("amount");
            if (amountLabel != null)
            {
                amountLabel.text = GetAmountText(flowerSeed);
            }

            // Enable button only if player has the seed
            bool hasFlower = InventoryManager.Instance != null && 
                           InventoryManager.Instance.HasItem(flowerSeed.seedName, ItemType.FlowerSeeds, 1);
            button.SetEnabled(hasFlower);
        }
    }

    public void SetUIVisible(bool visible)
    {
        if (plantingPanel != null)
        {
            plantingPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            isUIVisible = visible;
        }

        // Update KeyboardInputManager state
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SetPlantingOpen(visible);
        }

        // Manage cursor visibility
        if (visible)
        {
            // Show cursor when UI opens
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            RefreshUI();
        }
        else
        {
            // Let KeyboardInputManager handle cursor state
            if (KeyboardInputManager.Instance != null)
            {
                KeyboardInputManager.Instance.SyncCursorState();
            }
        }
    }

    public void ToggleUI()
    {
        bool currentlyVisible = plantingPanel?.style.display == DisplayStyle.Flex;
        SetUIVisible(!currentlyVisible);
    }

    public bool IsUIVisible() => isUIVisible;
}
