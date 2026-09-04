#if STEAM_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Arborvale.Shared;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Full gacha screen. States: BannerBrowse → Confirm → Pulling → Reveal → Results.
/// Opens when the player interacts with the Wishing Well object (set via ShowGachaUI).
/// Attach to a persistent GameObject in Eden_Shared.unity with a UIDocument.
///
/// Client never computes drop rates — all returned by server.
/// Pity counters are displayed from server response only.
/// </summary>
public class GachaUIManager : MonoBehaviour
{
    public static GachaUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private enum GachaScreenState { Hidden, BannerBrowse, Confirm, Pulling, Reveal, Results }

    private GachaScreenState screenState = GachaScreenState.Hidden;

    // ── Root panels ────────────────────────────────────────────────────
    private VisualElement root;
    private VisualElement gachaRoot;
    private VisualElement browsePanelEl;
    private VisualElement confirmPanelEl;
    private VisualElement pullingPanelEl;
    private VisualElement revealPanelEl;
    private VisualElement resultsPanelEl;

    // ── Browse panel ───────────────────────────────────────────────────
    private VisualElement bannerCarousel;
    private Label bannerNameLabel;
    private Label bannerDescLabel;
    private Label oddsLabel;
    private Label rotationLabel;
    private Label pityLabel;

    // ── Confirm panel ──────────────────────────────────────────────────
    private Label confirmBannerLabel;
    private Label confirmCostLabel;
    private Button pullOneButton;
    private Button pullTenButton;
    private Button premiumToggle;
    private Button freeToggle;

    // ── Reveal panel ───────────────────────────────────────────────────
    private VisualElement revealContainer;

    // ── Results panel ──────────────────────────────────────────────────
    private VisualElement resultsGrid;

    // ── State ──────────────────────────────────────────────────────────
    private List<BannerDto> loadedBanners = new List<BannerDto>();
    private int selectedBannerIndex = 0;
    private CurrencyType selectedCurrency = CurrencyType.Premium;
    private GachaPullResultDto lastPullResult;
    private string pendingIdempotencyKey;

    // ── Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        BuildUI();
    }

    private void Update()
    {
        if (screenState != GachaScreenState.Hidden &&
            UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            if (screenState == GachaScreenState.BannerBrowse || screenState == GachaScreenState.Results)
                HideGachaUI();
        }
    }

    // ── Public entry point ─────────────────────────────────────────────

    public void ShowGachaUI()
    {
        if (!OnlineServices.IsAvailable)
        {
            HUDManager.Instance?.ShowMessage("Gacha requires an online connection.");
            return;
        }

        gachaRoot.style.display = DisplayStyle.Flex;
        screenState = GachaScreenState.BannerBrowse;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f;
        KeyboardInputManager.Instance?.SetGraftingOpen(true); // reuse to block other input

        ShowPanel(browsePanelEl);
        StartCoroutine(LoadBannersRoutine());
    }

    public void HideGachaUI()
    {
        gachaRoot.style.display = DisplayStyle.None;
        screenState = GachaScreenState.Hidden;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        Time.timeScale = 1f;
        KeyboardInputManager.Instance?.SetGraftingOpen(false);
    }

    // ── Banner loading ─────────────────────────────────────────────────

    private IEnumerator LoadBannersRoutine()
    {
        bannerCarousel.Clear();
        bannerNameLabel.text = "Loading...";

        var task = OnlineServices.Instance.GetBannersAsync();
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            bannerNameLabel.text = "Could not load banners.";
            yield break;
        }

        loadedBanners = new List<BannerDto>(task.Result?.Banners ?? Array.Empty<BannerDto>());
        selectedBannerIndex = 0;

        if (loadedBanners.Count == 0)
        {
            bannerNameLabel.text = "No banners available.";
            yield break;
        }

        // Also refresh wallet
        var walletTask = OnlineServices.Instance.GetWalletAsync();
        while (!walletTask.IsCompleted) yield return null;
        if (!walletTask.IsFaulted) WalletWidget.Instance?.Refresh(walletTask.Result);

        RenderBannerCarousel();
        SelectBanner(0);

        // Load pity counters
        yield return LoadPityRoutine();
    }

    private IEnumerator LoadPityRoutine()
    {
        // Pity data is returned inline with banner data or from grants endpoint
        // For v1 we show it from the banner itself if the server embeds it
        if (pityLabel != null) pityLabel.text = string.Empty;
        yield return null;
    }

    private void RenderBannerCarousel()
    {
        bannerCarousel.Clear();
        for (int i = 0; i < loadedBanners.Count; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectBanner(idx)) { text = loadedBanners[i].Name };
            btn.style.marginRight = 6;
            bannerCarousel.Add(btn);
        }
    }

    private void SelectBanner(int index)
    {
        if (index < 0 || index >= loadedBanners.Count) return;
        selectedBannerIndex = index;
        var banner = loadedBanners[index];

        bannerNameLabel.text = banner.Name;
        bannerDescLabel.text = banner.Description ?? string.Empty;
        oddsLabel.text = banner.DisplayedOddsText ?? string.Empty; // server-provided, never client-computed

        if (banner.EndsAtUtc != default)
        {
            var remaining = banner.EndsAtUtc - DateTime.UtcNow;
            rotationLabel.text = remaining.TotalSeconds > 0
                ? $"Ends in {FormatDuration(remaining)}"
                : "Ending soon";
        }
        else
        {
            rotationLabel.text = string.Empty;
        }
    }

    // ── Confirm panel ──────────────────────────────────────────────────

    private void OpenConfirmPanel()
    {
        if (loadedBanners.Count == 0) return;
        var banner = loadedBanners[selectedBannerIndex];
        confirmBannerLabel.text = banner.Name;
        UpdateConfirmCostLabel(1);
        ShowPanel(confirmPanelEl);
        screenState = GachaScreenState.Confirm;
    }

    private void UpdateConfirmCostLabel(int count)
    {
        confirmCostLabel.text = $"Pull ×{count}  [{selectedCurrency}]";
    }

    // ── Pull ───────────────────────────────────────────────────────────

    private void StartPull(int count)
    {
        if (screenState != GachaScreenState.Confirm) return;
        if (loadedBanners.Count == 0) return;

        pendingIdempotencyKey = Guid.NewGuid().ToString();
        ShowPanel(pullingPanelEl);
        screenState = GachaScreenState.Pulling;
        StartCoroutine(PullRoutine(loadedBanners[selectedBannerIndex].BannerId, count));
    }

    private IEnumerator PullRoutine(string bannerId, int count)
    {
        var task = OnlineServices.Instance.PullAsync(bannerId, count, selectedCurrency, pendingIdempotencyKey);
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            Debug.LogWarning($"[GachaUIManager] Pull failed: {task.Exception?.GetBaseException().Message}");
            HUDManager.Instance?.ShowMessage("Pull failed. Please try again.");
            ShowPanel(confirmPanelEl);
            screenState = GachaScreenState.Confirm;
            yield break;
        }

        lastPullResult = task.Result;

        // Refresh wallet from response
        if (lastPullResult?.Wallet != null)
            WalletWidget.Instance?.Refresh(lastPullResult.Wallet);

        // Grant items into local systems
        ApplyPullResults(lastPullResult);

        StartCoroutine(RevealRoutine(lastPullResult));
    }

    private void ApplyPullResults(GachaPullResultDto result)
    {
        if (result?.Results == null) return;
        var catalog = GachaItemCatalog.Instance;

        foreach (var item in result.Results)
        {
            var entry = catalog?.GetEntry(item.ItemId) ?? default;

            if (!string.IsNullOrEmpty(entry.seedName))
            {
                // Common: add directly to inventory
                InventoryManager.Instance?.AddItem(entry.seedName, ItemType.Seed, 1);
            }

            // Rare/4★: track grantId for trading
            if (!string.IsNullOrEmpty(item.GrantId))
                GrantStore.Instance?.AddGrant(item.ItemId, item.GrantId);
        }
    }

    // ── Reveal ─────────────────────────────────────────────────────────

    private IEnumerator RevealRoutine(GachaPullResultDto result)
    {
        ShowPanel(revealPanelEl);
        screenState = GachaScreenState.Reveal;
        revealContainer.Clear();

        if (result?.Results == null) { ShowResultsPanel(); yield break; }

        var catalog = GachaItemCatalog.Instance;
        bool skipped = false;
        var skipButton = revealPanelEl.Q<Button>("SkipReveal");
        if (skipButton != null) skipButton.clicked += () => skipped = true;

        foreach (var item in result.Results)
        {
            if (skipped) break;

            var entry = catalog?.GetEntry(item.ItemId) ?? default;
            var card = BuildRevealCard(entry.displayName ?? item.ItemId, entry.tier);
            revealContainer.Add(card);

            // Brief reveal delay per item (skippable)
            float elapsed = 0f;
            while (elapsed < 0.35f && !skipped)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // If skipped, fill remaining cards instantly
        if (skipped && result.Results != null)
        {
            revealContainer.Clear();
            foreach (var item in result.Results)
            {
                var entry = catalog?.GetEntry(item.ItemId) ?? default;
                revealContainer.Add(BuildRevealCard(entry.displayName ?? item.ItemId, entry.tier));
            }
        }

        yield return new WaitForSecondsRealtime(1.2f);
        ShowResultsPanel();
    }

    private VisualElement BuildRevealCard(string name, GachaItemCatalog.ItemTier tier)
    {
        var card = new VisualElement();
        card.style.width = 90;
        card.style.height = 120;
        card.style.marginRight = 8;
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
        card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
        card.style.alignItems = Align.Center;
        card.style.justifyContent = Justify.Center;

        card.style.backgroundColor = tier switch
        {
            GachaItemCatalog.ItemTier.FourStar => new StyleColor(new Color(0.8f, 0.65f, 0.05f)),
            GachaItemCatalog.ItemTier.Rare     => new StyleColor(new Color(0.6f, 0.1f, 0.7f)),
            _                                  => new StyleColor(new Color(0.15f, 0.45f, 0.2f))
        };

        var label = new Label(name);
        label.style.color = new StyleColor(Color.white);
        label.style.fontSize = 11;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.Normal;
        card.Add(label);

        return card;
    }

    private void ShowResultsPanel()
    {
        ShowPanel(resultsPanelEl);
        screenState = GachaScreenState.Results;

        resultsGrid.Clear();
        if (lastPullResult?.Results == null) return;

        var catalog = GachaItemCatalog.Instance;
        foreach (var item in lastPullResult.Results)
        {
            var entry = catalog?.GetEntry(item.ItemId) ?? default;
            resultsGrid.Add(BuildRevealCard(entry.displayName ?? item.ItemId, entry.tier));
        }
    }

    // ── UI Build ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        gachaRoot = new VisualElement { name = "GachaRoot" };
        gachaRoot.style.position = Position.Absolute;
        gachaRoot.style.left = gachaRoot.style.right = gachaRoot.style.top = gachaRoot.style.bottom = 0;
        gachaRoot.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.7f));
        gachaRoot.style.alignItems = Align.Center;
        gachaRoot.style.justifyContent = Justify.Center;
        gachaRoot.style.display = DisplayStyle.None;
        root.Add(gachaRoot);

        browsePanelEl = BuildBrowsePanel();
        confirmPanelEl = BuildConfirmPanel();
        pullingPanelEl = BuildPullingPanel();
        revealPanelEl = BuildRevealPanel();
        resultsPanelEl = BuildResultsPanel();

        foreach (var p in new[] { browsePanelEl, confirmPanelEl, pullingPanelEl, revealPanelEl, resultsPanelEl })
        {
            ApplyGlassStyle(p);
            p.style.width = 680;
            p.style.paddingTop = p.style.paddingBottom = p.style.paddingLeft = p.style.paddingRight = 28;
            gachaRoot.Add(p);
        }

        HideAllPanels();
    }

    private VisualElement BuildBrowsePanel()
    {
        var p = new VisualElement { name = "BrowsePanel" };

        var title = new Label("Wishing Well");
        StyleLabel(title, 26, bold: true);
        p.Add(title);

        bannerCarousel = new VisualElement();
        bannerCarousel.style.flexDirection = FlexDirection.Row;
        bannerCarousel.style.marginTop = 12;
        bannerCarousel.style.flexWrap = Wrap.Wrap;
        p.Add(bannerCarousel);

        bannerNameLabel = new Label(string.Empty);
        StyleLabel(bannerNameLabel, 20, bold: true);
        bannerNameLabel.style.marginTop = 14;
        p.Add(bannerNameLabel);

        bannerDescLabel = new Label(string.Empty);
        StyleLabel(bannerDescLabel, 14);
        bannerDescLabel.style.whiteSpace = WhiteSpace.Normal;
        bannerDescLabel.style.marginTop = 6;
        p.Add(bannerDescLabel);

        oddsLabel = new Label(string.Empty);
        StyleLabel(oddsLabel, 13);
        oddsLabel.style.color = new StyleColor(new Color(0.7f, 1f, 0.8f));
        oddsLabel.style.marginTop = 6;
        p.Add(oddsLabel);

        rotationLabel = new Label(string.Empty);
        StyleLabel(rotationLabel, 13);
        rotationLabel.style.color = new StyleColor(new Color(1f, 0.8f, 0.4f));
        rotationLabel.style.marginTop = 4;
        p.Add(rotationLabel);

        pityLabel = new Label(string.Empty);
        StyleLabel(pityLabel, 13);
        pityLabel.style.marginTop = 4;
        p.Add(pityLabel);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 20;

        var pullBtn = MakeButton("Pull", OpenConfirmPanel);
        StyleButtonPrimary(pullBtn);
        var closeBtn = MakeButton("Close", HideGachaUI);
        row.Add(pullBtn);
        row.Add(closeBtn);
        p.Add(row);

        return p;
    }

    private VisualElement BuildConfirmPanel()
    {
        var p = new VisualElement { name = "ConfirmPanel" };

        confirmBannerLabel = new Label(string.Empty);
        StyleLabel(confirmBannerLabel, 20, bold: true);
        p.Add(confirmBannerLabel);

        // Currency selector
        var currRow = new VisualElement();
        currRow.style.flexDirection = FlexDirection.Row;
        currRow.style.marginTop = 14;

        premiumToggle = MakeButton("★ Premium", () => { selectedCurrency = CurrencyType.Premium; UpdateConfirmCostLabel(1); });
        freeToggle = MakeButton("◆ Free", () => { selectedCurrency = CurrencyType.BloomShards; UpdateConfirmCostLabel(1); });
        StyleButtonPrimary(premiumToggle);
        currRow.Add(premiumToggle);
        currRow.Add(freeToggle);
        p.Add(currRow);

        confirmCostLabel = new Label(string.Empty);
        StyleLabel(confirmCostLabel, 16);
        confirmCostLabel.style.marginTop = 10;
        p.Add(confirmCostLabel);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 16;

        pullOneButton = MakeButton("Pull ×1", () => StartPull(1));
        pullTenButton = MakeButton("Pull ×10", () => StartPull(10));
        var backBtn = MakeButton("Back", () => { ShowPanel(browsePanelEl); screenState = GachaScreenState.BannerBrowse; });

        StyleButtonPrimary(pullOneButton);
        StyleButtonPrimary(pullTenButton);
        row.Add(pullOneButton);
        row.Add(pullTenButton);
        row.Add(backBtn);
        p.Add(row);

        return p;
    }

    private VisualElement BuildPullingPanel()
    {
        var p = new VisualElement { name = "PullingPanel" };
        var lbl = new Label("Pulling...") { name = "PullingLabel" };
        StyleLabel(lbl, 22, bold: true);
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        p.Add(lbl);
        return p;
    }

    private VisualElement BuildRevealPanel()
    {
        var p = new VisualElement { name = "RevealPanel" };

        var title = new Label("You got...");
        StyleLabel(title, 22, bold: true);
        p.Add(title);

        revealContainer = new VisualElement();
        revealContainer.style.flexDirection = FlexDirection.Row;
        revealContainer.style.flexWrap = Wrap.Wrap;
        revealContainer.style.marginTop = 14;
        p.Add(revealContainer);

        var skipBtn = new Button { name = "SkipReveal", text = "Skip" };
        skipBtn.style.marginTop = 16;
        p.Add(skipBtn);

        return p;
    }

    private VisualElement BuildResultsPanel()
    {
        var p = new VisualElement { name = "ResultsPanel" };

        var title = new Label("Results");
        StyleLabel(title, 22, bold: true);
        p.Add(title);

        resultsGrid = new VisualElement();
        resultsGrid.style.flexDirection = FlexDirection.Row;
        resultsGrid.style.flexWrap = Wrap.Wrap;
        resultsGrid.style.marginTop = 14;
        p.Add(resultsGrid);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 16;

        var pullAgain = MakeButton("Pull Again", OpenConfirmPanel);
        var done = MakeButton("Done", HideGachaUI);
        StyleButtonPrimary(pullAgain);
        row.Add(pullAgain);
        row.Add(done);
        p.Add(row);

        return p;
    }

    // ── Panel helpers ──────────────────────────────────────────────────

    private void ShowPanel(VisualElement target)
    {
        HideAllPanels();
        if (target != null) target.style.display = DisplayStyle.Flex;
    }

    private void HideAllPanels()
    {
        foreach (var p in new[] { browsePanelEl, confirmPanelEl, pullingPanelEl, revealPanelEl, resultsPanelEl })
            if (p != null) p.style.display = DisplayStyle.None;
    }

    // ── Styling helpers ────────────────────────────────────────────────

    private static void ApplyGlassStyle(VisualElement el)
    {
        el.style.backgroundColor = new StyleColor(new Color(0.04f, 0.12f, 0.06f, 0.94f));
        el.style.borderTopLeftRadius = el.style.borderTopRightRadius =
        el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 12;
        el.style.borderTopWidth = el.style.borderRightWidth =
        el.style.borderBottomWidth = el.style.borderLeftWidth = 1;
        el.style.borderTopColor = el.style.borderRightColor =
        el.style.borderBottomColor = el.style.borderLeftColor =
            new StyleColor(new Color(0.3f, 0.7f, 0.4f, 0.5f));
    }

    private static void StyleLabel(Label l, int size, bool bold = false)
    {
        l.style.color = new StyleColor(Color.white);
        l.style.fontSize = size;
        if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    private static Button MakeButton(string text, Action onClick)
    {
        var btn = new Button(onClick) { text = text };
        btn.style.marginRight = 8;
        btn.style.paddingLeft = btn.style.paddingRight = 16;
        btn.style.paddingTop = btn.style.paddingBottom = 8;
        return btn;
    }

    private static void StyleButtonPrimary(Button btn)
    {
        btn.style.backgroundColor = new StyleColor(new Color(0.18f, 0.55f, 0.28f));
        btn.style.color = new StyleColor(Color.white);
    }

    private static string FormatDuration(TimeSpan t)
    {
        if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h";
        if (t.TotalHours >= 1) return $"{t.Hours}h {t.Minutes}m";
        return $"{t.Minutes}m";
    }
}
#endif
