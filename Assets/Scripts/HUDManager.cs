using UnityEngine;
using UnityEngine.UIElements;

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
    }

    private void InitializeUI()
    {
        if (root == null) return;

        healthFill = root.Q<VisualElement>("HealthFill");
        staminaFill = root.Q<VisualElement>("StaminaFill");
        timeLabel = root.Q<Label>("TimeLabel");
        
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
