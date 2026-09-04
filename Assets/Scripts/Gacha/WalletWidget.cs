using Arborvale.Shared;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD widget showing dual currency balances (premium + free).
/// Refreshed by GachaUIManager after every mutating server response.
/// Attach to the HUD UIDocument's parent GameObject.
/// </summary>
public class WalletWidget : MonoBehaviour
{
    public static WalletWidget Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private Label premiumLabel;
    private Label freeLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        BuildWidget();
    }

    private void BuildWidget()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        var container = root.Q<VisualElement>("WalletWidget");
        if (container == null)
        {
            container = new VisualElement { name = "WalletWidget" };
            container.style.position = Position.Absolute;
            container.style.top = 12;
            container.style.right = 12;
            container.style.flexDirection = FlexDirection.Row;
            container.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.55f));
            container.style.paddingLeft = container.style.paddingRight = 10;
            container.style.paddingTop = container.style.paddingBottom = 6;
            container.style.borderTopLeftRadius = container.style.borderTopRightRadius =
            container.style.borderBottomLeftRadius = container.style.borderBottomRightRadius = 6;
            root.Add(container);
        }

        premiumLabel = new Label("★ --") { name = "PremiumBalance" };
        premiumLabel.style.color = new StyleColor(new Color(1f, 0.85f, 0.2f));
        premiumLabel.style.fontSize = 14;
        premiumLabel.style.marginRight = 14;

        freeLabel = new Label("◆ --") { name = "FreeBalance" };
        freeLabel.style.color = new StyleColor(new Color(0.4f, 0.9f, 0.5f));
        freeLabel.style.fontSize = 14;

        container.Add(premiumLabel);
        container.Add(freeLabel);

        // Hide until online
        container.style.display = DisplayStyle.None;
    }

    /// <summary>Called by GachaUIManager after every response that includes wallet data.</summary>
    public void Refresh(WalletDto wallet)
    {
        if (wallet == null) return;

        if (premiumLabel != null)
            premiumLabel.text = $"★ {wallet.Premium:N0}";
        if (freeLabel != null)
            freeLabel.text = $"◆ {wallet.BloomShards:N0}";

        // Show the widget
        var container = uiDocument?.rootVisualElement?.Q<VisualElement>("WalletWidget");
        if (container != null) container.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        var container = uiDocument?.rootVisualElement?.Q<VisualElement>("WalletWidget");
        if (container != null) container.style.display = DisplayStyle.None;
    }
}
