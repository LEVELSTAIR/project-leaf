using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Manages keyboard inputs for game actions like inventory, interact, use item, etc.
/// Dispatches events that other systems can subscribe to.
/// </summary>
public class KeyboardInputManager : MonoBehaviour
{
    public static KeyboardInputManager Instance { get; private set; }

    #region Events
    // Panel/Menu Events
    public event Action OnInventoryToggle;            // I key
    public event Action OnInteract;                   // F key
    public event Action OnUseItem;                    // E key
    public event Action OnInspectPlant;               // Q key
    public event Action OnBookLogToggle;              // B key
    public event Action OnMapEnlarge;                 // M key
    public event Action OnCraftToggle;                // C key
    public event Action OnEscapeMenuToggle;           // ESC key

    // Hotbar Events
    public event Action<int> OnHotbarSlotSelected;    // 1-5 keys
    #endregion

    #region Key Bindings (Configurable in Inspector)
    [Header("Panel Keybindings")]
    public Key inventoryKey = Key.I;
    public Key interactKey = Key.F;
    public Key useItemKey = Key.E;
    public Key inspectPlantKey = Key.Q;
    public Key bookLogKey = Key.B;
    public Key mapEnlargeKey = Key.M;
    public Key craftKey = Key.C;
    public Key escapeMenuKey = Key.Escape;

    [Header("Hotbar Keybindings")]
    public Key hotbarSlot1 = Key.Digit1;
    public Key hotbarSlot2 = Key.Digit2;
    public Key hotbarSlot3 = Key.Digit3;
    public Key hotbarSlot4 = Key.Digit4;
    public Key hotbarSlot5 = Key.Digit5;
    #endregion

    #region State Tracking
    [Header("State")]
    [SerializeField] private bool isInventoryOpen = false;
    [SerializeField] private bool isBookLogOpen = false;
    [SerializeField] private bool isMapEnlarged = false;
    [SerializeField] private bool isCraftOpen = false;
    [SerializeField] private bool isEscapeMenuOpen = false;
    [SerializeField] private int currentHotbarSlot = 1;

    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsBookLogOpen => isBookLogOpen;
    public bool IsMapEnlarged => isMapEnlarged;
    public bool IsCraftOpen => isCraftOpen;
    public bool IsEscapeMenuOpen => isEscapeMenuOpen;
    public int CurrentHotbarSlot => currentHotbarSlot;
    public bool IsAnyPanelOpen => isInventoryOpen || isBookLogOpen || isMapEnlarged || isCraftOpen || isEscapeMenuOpen;
    #endregion

    #region Escape Menu UI References
    [Header("Escape Menu UI")]
    public UIDocument escapeMenuDocument;
    private VisualElement escapeMenuRoot;
    private VisualElement escapeMenuPanel;
    #endregion

    private Keyboard keyboard;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        keyboard = Keyboard.current;

        // Initialize Escape Menu UI if assigned
        if (escapeMenuDocument != null)
        {
            escapeMenuRoot = escapeMenuDocument.rootVisualElement;
            escapeMenuPanel = escapeMenuRoot?.Q<VisualElement>("EscapeMenuPanel");
            
            // Initially hide the escape menu
            if (escapeMenuPanel != null)
            {
                escapeMenuPanel.style.display = DisplayStyle.None;
            }

            // Hook up button callbacks
            SetupEscapeMenuCallbacks();
        }
    }

    private void Update()
    {
        if (keyboard == null)
            keyboard = Keyboard.current;

        if (keyboard == null) return;

        // Handle Panel Inputs
        HandlePanelInputs();

        // Handle Hotbar Inputs
        HandleHotbarInputs();
    }

    private void HandlePanelInputs()
    {
        // Inventory (I)
        if (WasKeyPressedThisFrame(inventoryKey))
        {
            isInventoryOpen = !isInventoryOpen;
            OnInventoryToggle?.Invoke();
            Debug.Log($"[Input] Inventory toggled: {isInventoryOpen}");
        }

        // Interact (F)
        if (WasKeyPressedThisFrame(interactKey))
        {
            OnInteract?.Invoke();
            Debug.Log("[Input] Interact pressed");
        }

        // Use Item (E)
        if (WasKeyPressedThisFrame(useItemKey))
        {
            OnUseItem?.Invoke();
            Debug.Log("[Input] Use Item pressed");
        }

        // Inspect Plant (Q)
        if (WasKeyPressedThisFrame(inspectPlantKey))
        {
            OnInspectPlant?.Invoke();
            Debug.Log("[Input] Inspect Plant pressed");
        }

        // Book Log (B)
        if (WasKeyPressedThisFrame(bookLogKey))
        {
            isBookLogOpen = !isBookLogOpen;
            OnBookLogToggle?.Invoke();
            Debug.Log($"[Input] Book Log toggled: {isBookLogOpen}");
        }

        // Map Enlarge (M)
        if (WasKeyPressedThisFrame(mapEnlargeKey))
        {
            isMapEnlarged = !isMapEnlarged;
            OnMapEnlarge?.Invoke();
            Debug.Log($"[Input] Map Enlarged toggled: {isMapEnlarged}");
        }

        // Craft (C)
        if (WasKeyPressedThisFrame(craftKey))
        {
            isCraftOpen = !isCraftOpen;
            OnCraftToggle?.Invoke();
            Debug.Log($"[Input] Craft toggled: {isCraftOpen}");
        }

        // Escape Menu (ESC)
        if (WasKeyPressedThisFrame(escapeMenuKey))
        {
            ToggleEscapeMenu();
        }
    }

    private void HandleHotbarInputs()
    {
        // Hotbar Slot 1
        if (WasKeyPressedThisFrame(hotbarSlot1))
        {
            SelectHotbarSlot(1);
        }

        // Hotbar Slot 2
        if (WasKeyPressedThisFrame(hotbarSlot2))
        {
            SelectHotbarSlot(2);
        }

        // Hotbar Slot 3
        if (WasKeyPressedThisFrame(hotbarSlot3))
        {
            SelectHotbarSlot(3);
        }

        // Hotbar Slot 4
        if (WasKeyPressedThisFrame(hotbarSlot4))
        {
            SelectHotbarSlot(4);
        }

        // Hotbar Slot 5
        if (WasKeyPressedThisFrame(hotbarSlot5))
        {
            SelectHotbarSlot(5);
        }
    }

    private void SelectHotbarSlot(int slot)
    {
        currentHotbarSlot = slot;
        OnHotbarSlotSelected?.Invoke(slot);
        Debug.Log($"[Input] Hotbar slot {slot} selected");
    }

    private bool WasKeyPressedThisFrame(Key key)
    {
        return keyboard[key].wasPressedThisFrame;
    }

    #region Escape Menu Management
    private void ToggleEscapeMenu()
    {
        isEscapeMenuOpen = !isEscapeMenuOpen;
        OnEscapeMenuToggle?.Invoke();
        
        if (escapeMenuPanel != null)
        {
            if (isEscapeMenuOpen)
            {
                ShowEscapeMenu();
            }
            else
            {
                HideEscapeMenu();
            }
        }

        // Toggle cursor lock state
        UnityEngine.Cursor.lockState = isEscapeMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = isEscapeMenuOpen;

        Debug.Log($"[Input] Escape Menu toggled: {isEscapeMenuOpen}");
    }

    private void ShowEscapeMenu()
    {
        if (escapeMenuPanel == null) return;
        
        escapeMenuPanel.style.display = DisplayStyle.Flex;
        // Trigger slide-in animation via class
        escapeMenuPanel.RemoveFromClassList("menu-hidden");
        escapeMenuPanel.AddToClassList("menu-visible");
    }

    private void HideEscapeMenu()
    {
        if (escapeMenuPanel == null) return;
        
        escapeMenuPanel.RemoveFromClassList("menu-visible");
        escapeMenuPanel.AddToClassList("menu-hidden");
        
        // Delay hiding to allow animation to complete
        escapeMenuPanel.schedule.Execute(() =>
        {
            if (!isEscapeMenuOpen && escapeMenuPanel != null)
            {
                escapeMenuPanel.style.display = DisplayStyle.None;
            }
        }).StartingIn(300); // Match animation duration
    }

    private void SetupEscapeMenuCallbacks()
    {
        if (escapeMenuRoot == null) return;

        // Resume Button
        var resumeBtn = escapeMenuRoot.Q<Button>("ResumeButton");
        resumeBtn?.RegisterCallback<ClickEvent>(evt => ToggleEscapeMenu());

        // Settings Button
        var settingsBtn = escapeMenuRoot.Q<Button>("SettingsButton");
        settingsBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[EscMenu] Settings clicked");
            // TODO: Open settings panel
        });

        // Save Button
        var saveBtn = escapeMenuRoot.Q<Button>("SaveButton");
        saveBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[EscMenu] Save clicked");
            // TODO: Trigger save game
        });

        // Load Button
        var loadBtn = escapeMenuRoot.Q<Button>("LoadButton");
        loadBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[EscMenu] Load clicked");
            // TODO: Open load menu
        });

        // Main Menu Button
        var mainMenuBtn = escapeMenuRoot.Q<Button>("MainMenuButton");
        mainMenuBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[EscMenu] Main Menu clicked");
            // TODO: Return to main menu
        });

        // Quit Button
        var quitBtn = escapeMenuRoot.Q<Button>("QuitButton");
        quitBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[EscMenu] Quit clicked");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        });
    }
    #endregion

    #region Public Methods for External Use
    /// <summary>
    /// Closes the escape menu if it's open. Useful for other systems to close it.
    /// </summary>
    public void CloseEscapeMenu()
    {
        if (isEscapeMenuOpen)
        {
            ToggleEscapeMenu();
        }
    }

    /// <summary>
    /// Opens the escape menu if it's closed. Useful for other systems to open it.
    /// </summary>
    public void OpenEscapeMenu()
    {
        if (!isEscapeMenuOpen)
        {
            ToggleEscapeMenu();
        }
    }

    /// <summary>
    /// Resets all panel states to closed.
    /// </summary>
    public void CloseAllPanels()
    {
        if (isInventoryOpen)
        {
            isInventoryOpen = false;
            OnInventoryToggle?.Invoke();
        }
        if (isBookLogOpen)
        {
            isBookLogOpen = false;
            OnBookLogToggle?.Invoke();
        }
        if (isMapEnlarged)
        {
            isMapEnlarged = false;
            OnMapEnlarge?.Invoke();
        }
        if (isCraftOpen)
        {
            isCraftOpen = false;
            OnCraftToggle?.Invoke();
        }
        if (isEscapeMenuOpen)
        {
            CloseEscapeMenu();
        }
    }
    #endregion
}
