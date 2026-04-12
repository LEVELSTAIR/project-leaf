// CraftingUIManager.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CraftingUIManager : MonoBehaviour
{
    public static CraftingUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Recipes")]
    public List<CraftingRecipe> recipesToDisplay;

    // UI Element references
    private VisualElement root;
    private VisualElement recipeListContainer;
    private VisualElement craftingPanel;
    private Button closeButton;

    // Keep track of recipe buttons for updating interactivity
    private Dictionary<CraftingRecipe, Button> recipeButtons = new Dictionary<CraftingRecipe, Button>();

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

        // Subscribe to keyboard input for toggling
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnCraftToggle += HandleCraftToggle;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
        }

        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnCraftToggle -= HandleCraftToggle;
        }
    }

    private void Start()
    {
        InitializeUI();
        BuildRecipeList();
        SetUIVisible(false); // Start hidden
    }

    private void HandleCraftToggle()
    {
        // If we're currently placing a crafted object, cancel placement instead of toggling UI
        if (CraftingController.Instance != null && CraftingController.Instance.IsCurrentlyPlacing())
        {
            CraftingController.Instance.CancelPlacement();
            return;
        }

        ToggleUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("CraftingUIManager: UIDocument not assigned!");
            return;
        }

        root = uiDocument.rootVisualElement;
        craftingPanel = root.Q<VisualElement>("crafting-panel");
        recipeListContainer = root.Q<VisualElement>("recipe-list");
        closeButton = root.Q<Button>("close-button");

        if (closeButton != null)
            closeButton.clicked += OnCloseButtonClicked;
    }

    private void OnCloseButtonClicked()
    {
        SetUIVisible(false);
        
        // Ensure crafting state is closed in KeyboardInputManager
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SetCraftOpen(false);
        }
    }

    private void BuildRecipeList()
    {
        if (recipeListContainer == null) return;

        recipeListContainer.Clear();
        recipeButtons.Clear();

        foreach (var recipe in recipesToDisplay)
        {
            var recipeEntry = CreateRecipeEntry(recipe);
            recipeListContainer.Add(recipeEntry);
        }
    }

    private VisualElement CreateRecipeEntry(CraftingRecipe recipe)
    {
        // Try to load template from Resources
        VisualTreeAsset template = Resources.Load<VisualTreeAsset>("Crafting/RecipeEntryTemplate");
        VisualElement entry;

        if (template != null)
        {
            entry = template.CloneTree();
        }
        else
        {
            // Fallback manual creation
            entry = new VisualElement();
            entry.AddToClassList("recipe-entry");

            var iconContainer = new VisualElement();
            iconContainer.AddToClassList("recipe-icon");
            entry.Add(iconContainer);

            var nameLabel = new Label(recipe.recipeName);
            nameLabel.AddToClassList("recipe-name");
            entry.Add(nameLabel);

            var requirementsLabel = new Label(GetRequirementsText(recipe));
            requirementsLabel.name = "requirements";
            requirementsLabel.AddToClassList("recipe-requirements");
            entry.Add(requirementsLabel);

            var craftButton = new Button(() => OnCraftClicked(recipe));
            craftButton.name = "craft-button";
            craftButton.text = "Craft";
            craftButton.AddToClassList("craft-button");
            entry.Add(craftButton);

            // Store button reference
            recipeButtons[recipe] = craftButton;

            return entry;
        }

        // Fill data from template
        var nameLabelT = entry.Q<Label>("recipe-name");
        if (nameLabelT != null) nameLabelT.text = recipe.recipeName;

        var iconElement = entry.Q<VisualElement>("recipe-icon");
        if (iconElement != null && recipe.icon != null)
            iconElement.style.backgroundImage = new StyleBackground(recipe.icon);

        var reqLabel = entry.Q<Label>("requirements");
        if (reqLabel != null) reqLabel.text = GetRequirementsText(recipe);

        var craftButtonT = entry.Q<Button>("craft-button");
        if (craftButtonT != null)
        {
            craftButtonT.clicked += () => OnCraftClicked(recipe);
            recipeButtons[recipe] = craftButtonT;
        }

        return entry;
    }

    private string GetRequirementsText(CraftingRecipe recipe)
    {
        string text = "";
        foreach (var req in recipe.requiredResources)
        {
            int current = InventoryManager.Instance.GetItemAmount(req.itemName, req.itemType);
            text += $"{req.itemName}: {current}/{req.amount}\n";
        }
        return text.TrimEnd('\n');
    }

    private void OnCraftClicked(CraftingRecipe recipe)
    {
        if (CraftingController.Instance != null)
        {
            // Close the crafting panel so cursor locks during placement
            SetUIVisible(false);
            
            // Mark crafting as closed in KeyboardInputManager
            if (KeyboardInputManager.Instance != null)
            {
                KeyboardInputManager.Instance.SetCraftOpen(false);
            }
            
            CraftingController.Instance.TryCraftRecipe(recipe);
        }
    }

    public void RefreshUI()
    {
        if (recipeListContainer == null) return;

        // Update requirements text and button interactability
        foreach (var kvp in recipeButtons)
        {
            var recipe = kvp.Key;
            var button = kvp.Value;

            var entry = button.parent;
            var reqLabel = entry.Q<Label>("requirements");
            if (reqLabel != null)
                reqLabel.text = GetRequirementsText(recipe);

            button.SetEnabled(recipe.CanCraft(InventoryManager.Instance));
        }
    }

    public void SetUIVisible(bool visible)
    {
        if (craftingPanel != null)
            craftingPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Sync with input manager state (optional)
        if (KeyboardInputManager.Instance != null)
        {
            // You can add a public setter in KeyboardInputManager if desired
            // KeyboardInputManager.Instance.SetCraftOpen(visible);
        }

        if (visible)
            RefreshUI();
    }

    public void ToggleUI()
    {
        bool currentlyVisible = craftingPanel?.style.display == DisplayStyle.Flex;
        SetUIVisible(!currentlyVisible);
    }
}
