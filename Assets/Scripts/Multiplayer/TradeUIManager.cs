#if STEAM_BUILD
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Programmatic Trade UI (UI Toolkit, follows GlassTheme).
/// States driven by TradeSession.OnStateChanged.
/// Attach to a GameObject in Eden_Shared.unity with a UIDocument component.
/// </summary>
public class TradeUIManager : MonoBehaviour
{
    public static TradeUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement tradePanel;
    private bool isVisible;

    // ── Panels ─────────────────────────────────────────────────────────
    private VisualElement invitePanel;
    private VisualElement negotiatePanel;
    private VisualElement resultPanel;

    // ── Negotiate panel refs ────────────────────────────────────────────
    private VisualElement myOfferList;
    private VisualElement theirOfferList;
    private Button acceptButton;
    private Label statusLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        BuildUI();

        if (TradeSession.Instance != null)
            TradeSession.Instance.OnStateChanged += OnTradeStateChanged;
    }

    private void OnDisable()
    {
        if (TradeSession.Instance != null)
            TradeSession.Instance.OnStateChanged -= OnTradeStateChanged;
    }

    // ── Public entry points ────────────────────────────────────────────

    public void ShowIncomingInvite(ulong fromClientId)
    {
        ShowPanel(invitePanel);
        var msg = invitePanel.Q<Label>("InviteMessage");
        if (msg != null) msg.text = $"Player {fromClientId} wants to trade.";
    }

    public void ShowResult(bool success, string reason)
    {
        ShowPanel(resultPanel);
        var msg = resultPanel.Q<Label>("ResultMessage");
        if (msg != null)
            msg.text = success ? "Trade complete!" : $"Trade failed: {reason}";
    }

    // ── TradeSession event handler ─────────────────────────────────────

    private void OnTradeStateChanged(TradeState state)
    {
        switch (state)
        {
            case TradeState.Idle:
            case TradeState.Cancelled:
                HideAll();
                KeyboardInputManager.Instance?.SetTradeOpen(false);
                break;

            case TradeState.Invited:
                // Recipient sees the invite panel; shown via ShowIncomingInvite ClientRpc
                break;

            case TradeState.Negotiating:
                ShowNegotiatePanel();
                KeyboardInputManager.Instance?.SetTradeOpen(true);
                break;

            case TradeState.BothAccepted:
                if (statusLabel != null) statusLabel.text = "Both accepted — locking in 3s...";
                if (acceptButton != null) acceptButton.SetEnabled(false);
                break;

            case TradeState.Committing:
                if (statusLabel != null) statusLabel.text = "Committing...";
                break;

            case TradeState.Completed:
                ShowResult(true, string.Empty);
                KeyboardInputManager.Instance?.SetTradeOpen(false);
                break;

            case TradeState.Failed:
                ShowResult(false, "Trade could not be completed.");
                KeyboardInputManager.Instance?.SetTradeOpen(false);
                break;
        }
    }

    // ── UI Build ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        tradePanel = new VisualElement { name = "TradePanel" };
        ApplyGlassStyle(tradePanel);
        tradePanel.style.position = Position.Absolute;
        tradePanel.style.left = new Length(50, LengthUnit.Percent);
        tradePanel.style.top = new Length(50, LengthUnit.Percent);
        tradePanel.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent)));
        tradePanel.style.width = 620;
        tradePanel.style.paddingLeft = tradePanel.style.paddingRight =
        tradePanel.style.paddingTop = tradePanel.style.paddingBottom = 16;
        tradePanel.style.display = DisplayStyle.None;
        root.Add(tradePanel);

        invitePanel = BuildInvitePanel();
        negotiatePanel = BuildNegotiatePanel();
        resultPanel = BuildResultPanel();

        tradePanel.Add(invitePanel);
        tradePanel.Add(negotiatePanel);
        tradePanel.Add(resultPanel);

        HideAll();
    }

    private VisualElement BuildInvitePanel()
    {
        var panel = new VisualElement { name = "InvitePanel" };
        ApplyPanelPadding(panel);

        var msg = new Label { name = "InviteMessage", text = "Someone wants to trade." };
        StyleLabel(msg, 18);
        panel.Add(msg);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 16;

        var accept = MakeButton("Accept", () =>
        {
            TradeSession.Instance?.RespondToInviteServerRpc(true);
            HidePanel(invitePanel);
        });
        var decline = MakeButton("Decline", () =>
        {
            TradeSession.Instance?.RespondToInviteServerRpc(false);
            HideAll();
        });

        StyleButtonPrimary(accept);
        row.Add(accept);
        row.Add(decline);
        panel.Add(row);
        return panel;
    }

    private VisualElement BuildNegotiatePanel()
    {
        var panel = new VisualElement { name = "NegotiatePanel" };
        ApplyPanelPadding(panel);

        var title = new Label("Trade");
        StyleLabel(title, 22, bold: true);
        panel.Add(title);

        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.marginTop = 12;

        var myCol = BuildOfferColumn("Your Offer", out myOfferList);
        var theirCol = BuildOfferColumn("Their Offer", out theirOfferList);
        columns.Add(myCol);
        columns.Add(theirCol);
        panel.Add(columns);

        statusLabel = new Label { name = "StatusLabel", text = string.Empty };
        StyleLabel(statusLabel, 14);
        statusLabel.style.marginTop = 8;
        panel.Add(statusLabel);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 12;

        acceptButton = MakeButton("Accept Trade", () =>
        {
            TradeSession.Instance?.AcceptOfferServerRpc();
            acceptButton.SetEnabled(false);
        });
        var cancelButton = MakeButton("Cancel", () => TradeSession.Instance?.CancelServerRpc());
        StyleButtonPrimary(acceptButton);

        row.Add(acceptButton);
        row.Add(cancelButton);
        panel.Add(row);
        return panel;
    }

    private VisualElement BuildOfferColumn(string title, out VisualElement itemList)
    {
        var col = new VisualElement();
        col.style.flexGrow = 1;
        col.style.marginRight = 8;

        var label = new Label(title);
        StyleLabel(label, 16, bold: true);
        col.Add(label);

        itemList = new VisualElement { name = title.Replace(" ", "") + "List" };
        itemList.style.marginTop = 6;
        col.Add(itemList);

        return col;
    }

    private VisualElement BuildResultPanel()
    {
        var panel = new VisualElement { name = "ResultPanel" };
        ApplyPanelPadding(panel);

        var msg = new Label { name = "ResultMessage", text = string.Empty };
        StyleLabel(msg, 20, bold: true);
        panel.Add(msg);

        var ok = MakeButton("OK", HideAll);
        ok.style.marginTop = 16;
        StyleButtonPrimary(ok);
        panel.Add(ok);

        return panel;
    }

    // ── Refresh offer columns ──────────────────────────────────────────

    private void ShowNegotiatePanel()
    {
        ShowPanel(negotiatePanel);
        RefreshOfferLists();

        if (TradeSession.Instance != null)
        {
            TradeSession.Instance.InitiatorOffer.OnListChanged += _ => RefreshOfferLists();
            TradeSession.Instance.RecipientOffer.OnListChanged += _ => RefreshOfferLists();
        }

        if (acceptButton != null) acceptButton.SetEnabled(true);
        if (statusLabel != null) statusLabel.text = string.Empty;
    }

    private void RefreshOfferLists()
    {
        if (TradeSession.Instance == null) return;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        RenderOffer(myOfferList, TradeSession.Instance.GetMyOffer(localId));

        bool iAmInitiator = localId == TradeSession.Instance.InitiatorId;
        var theirOffer = iAmInitiator
            ? TradeSession.Instance.RecipientOffer
            : TradeSession.Instance.InitiatorOffer;
        RenderOffer(theirOfferList, theirOffer);
    }

    private void RenderOffer(VisualElement container, NetworkList<TradeItem> items)
    {
        if (container == null) return;
        container.Clear();

        if (items.Count == 0)
        {
            var empty = new Label("(nothing)");
            empty.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            container.Add(empty);
            return;
        }

        foreach (var item in items)
        {
            var row = new Label($"{item.itemName} ×{item.quantity}{(item.hasGrant ? " [grant]" : string.Empty)}");
            StyleLabel(row, 14);
            container.Add(row);
        }
    }

    // ── Panel visibility helpers ───────────────────────────────────────

    private void ShowPanel(VisualElement panel)
    {
        if (tradePanel == null) return;
        tradePanel.style.display = DisplayStyle.Flex;
        invitePanel.style.display = DisplayStyle.None;
        negotiatePanel.style.display = DisplayStyle.None;
        resultPanel.style.display = DisplayStyle.None;
        if (panel != null) panel.style.display = DisplayStyle.Flex;
        isVisible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void HidePanel(VisualElement panel)
    {
        if (panel != null) panel.style.display = DisplayStyle.None;
    }

    private void HideAll()
    {
        if (tradePanel != null) tradePanel.style.display = DisplayStyle.None;
        isVisible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        Time.timeScale = 1f;
    }

    // ── Styling helpers (matches GlassTheme dark-green palette) ────────

    private static void ApplyGlassStyle(VisualElement el)
    {
        el.style.backgroundColor = new StyleColor(new Color(0.05f, 0.15f, 0.08f, 0.92f));
        el.style.borderTopLeftRadius = el.style.borderTopRightRadius =
        el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 10;
        el.style.borderTopWidth = el.style.borderRightWidth =
        el.style.borderBottomWidth = el.style.borderLeftWidth = 1;
        el.style.borderTopColor = el.style.borderRightColor =
        el.style.borderBottomColor = el.style.borderLeftColor =
            new StyleColor(new Color(0.3f, 0.7f, 0.4f, 0.5f));
    }

    private static void ApplyPanelPadding(VisualElement el)
    {
        el.style.paddingTop = el.style.paddingBottom =
        el.style.paddingLeft = el.style.paddingRight = 24;
    }

    private static void StyleLabel(Label l, int size, bool bold = false)
    {
        l.style.color = new StyleColor(Color.white);
        l.style.fontSize = size;
        if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    private static Button MakeButton(string text, System.Action onClick)
    {
        var btn = new Button(onClick) { text = text };
        btn.style.marginRight = 8;
        btn.style.paddingLeft = btn.style.paddingRight = 16;
        btn.style.paddingTop = btn.style.paddingBottom = 8;
        return btn;
    }

    private static void StyleButtonPrimary(Button btn)
    {
        btn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.3f));
        btn.style.color = new StyleColor(Color.white);
    }
}
#endif
