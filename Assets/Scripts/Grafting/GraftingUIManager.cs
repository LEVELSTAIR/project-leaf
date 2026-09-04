using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the programmatic Graft Bench UI panel (UI Toolkit, follows GlassTheme).
/// Shows available recipes derived from the player's branch inventory.
/// </summary>
public class GraftingUIManager : MonoBehaviour
{
    public static GraftingUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement graftPanel;
    private VisualElement recipeContainer;
    private GraftingBench currentBench;
    private bool isUIVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        InitializeUI();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;

        if (isUIVisible)
            HideGraftingUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        graftPanel = root.Q<VisualElement>("GraftingPanel");
        if (graftPanel == null)
            CreateGraftPanel();
        else
            recipeContainer = graftPanel.Q<VisualElement>("RecipeContainer");

        if (graftPanel != null)
            graftPanel.style.display = DisplayStyle.None;
    }

    private void CreateGraftPanel()
    {
        graftPanel = new VisualElement();
        graftPanel.name = "GraftingPanel";
        graftPanel.AddToClassList("glass-panel");
        graftPanel.style.position = Position.Absolute;
        graftPanel.style.top = 0; graftPanel.style.left = 0;
        graftPanel.style.right = 0; graftPanel.style.bottom = 0;
        graftPanel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        graftPanel.style.justifyContent = Justify.Center;
        graftPanel.style.alignItems = Align.Center;
        graftPanel.style.display = DisplayStyle.None;

        var inner = new VisualElement();
        inner.style.width = 700; inner.style.minHeight = 400;
        inner.style.backgroundColor = new Color(0.08f, 0.12f, 0.08f, 0.97f);
        inner.style.borderTopLeftRadius = 16; inner.style.borderTopRightRadius = 16;
        inner.style.borderBottomLeftRadius = 16; inner.style.borderBottomRightRadius = 16;
        inner.style.borderTopWidth = 2; inner.style.borderRightWidth = 2;
        inner.style.borderBottomWidth = 2; inner.style.borderLeftWidth = 2;
        inner.style.borderTopColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        inner.style.borderRightColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        inner.style.borderBottomColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        inner.style.borderLeftColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        inner.style.paddingTop = 24; inner.style.paddingBottom = 24;
        inner.style.paddingLeft = 24; inner.style.paddingRight = 24;
        inner.style.flexDirection = FlexDirection.Column;

        var title = new Label("Graft Bench");
        title.style.fontSize = 28; title.style.color = new Color(0.6f, 1f, 0.6f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 16;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        inner.Add(title);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1; scroll.style.marginBottom = 12;

        recipeContainer = new VisualElement();
        recipeContainer.name = "RecipeContainer";
        recipeContainer.style.flexDirection = FlexDirection.Column;
        scroll.Add(recipeContainer);
        inner.Add(scroll);

        var closeBtn = new Button(HideGraftingUI);
        closeBtn.text = "Close";
        closeBtn.style.alignSelf = Align.Center;
        closeBtn.style.width = 120; closeBtn.style.height = 36;
        closeBtn.style.fontSize = 16;
        inner.Add(closeBtn);

        graftPanel.Add(inner);
        root.Add(graftPanel);
    }

    public void ShowGraftingUI(GraftingBench bench)
    {
        if (graftPanel == null) InitializeUI();
        currentBench = bench;
        RefreshUI();
        graftPanel.style.display = DisplayStyle.Flex;
        isUIVisible = true;
        KeyboardInputManager.Instance?.SetGraftingOpen(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void HideGraftingUI()
    {
        if (graftPanel != null)
            graftPanel.style.display = DisplayStyle.None;
        currentBench = null;
        isUIVisible = false;
        Time.timeScale = 1f;
        KeyboardInputManager.Instance?.SetGraftingOpen(false);
    }

    private void RefreshUI()
    {
        if (!isUIVisible || recipeContainer == null || currentBench == null) return;
        recipeContainer.Clear();

        var available = currentBench.GetAvailableRecipes();
        if (available.Count == 0)
        {
            var msg = new Label("No compatible branch pairs in inventory.\nBreak branches from plants in the Early growth stage.");
            msg.style.color = new Color(0.8f, 0.8f, 0.6f);
            msg.style.fontSize = 15;
            msg.style.whiteSpace = WhiteSpace.Normal;
            msg.style.unityTextAlign = TextAnchor.MiddleCenter;
            msg.style.paddingTop = 24;
            recipeContainer.Add(msg);
            return;
        }

        foreach (var recipe in available)
            recipeContainer.Add(CreateRecipeRow(recipe));
    }

    private VisualElement CreateRecipeRow(GraftRecipe recipe)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 10;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.paddingLeft = 12; row.style.paddingRight = 12;
        row.style.backgroundColor = new Color(0.1f, 0.16f, 0.1f, 0.8f);
        row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
        row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;

        var info = new VisualElement();
        info.style.flexGrow = 1; info.style.flexDirection = FlexDirection.Column;

        var nameLabel = new Label($"{recipe.speciesA.seedName}  +  {recipe.speciesB.seedName}  →  {recipe.resultSeedData?.seedName ?? "Hybrid"}");
        nameLabel.style.color = Color.white; nameLabel.style.fontSize = 16;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        string fertName = recipe.GetFertilizerItemName();
        int fertAmt = recipe.GetFertilizerAmount();
        string costLine = fertAmt > 0 ? $"Cost: {fertAmt}x {fertName}  ·  Time: {recipe.graftTimeSeconds}s" : $"Time: {recipe.graftTimeSeconds}s";
        var costLabel = new Label(costLine);
        costLabel.style.color = new Color(0.7f, 0.9f, 0.7f); costLabel.style.fontSize = 13;

        info.Add(nameLabel); info.Add(costLabel);
        row.Add(info);

        var graftBtn = new Button(() =>
        {
            HideGraftingUI();
            currentBench?.StartGraft(recipe, 0);
        });
        graftBtn.text = "Graft";
        graftBtn.style.width = 80; graftBtn.style.height = 36; graftBtn.style.fontSize = 15;
        row.Add(graftBtn);

        return row;
    }

    private void Update()
    {
        if (!isUIVisible) return;
        if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            HideGraftingUI();
    }
}
