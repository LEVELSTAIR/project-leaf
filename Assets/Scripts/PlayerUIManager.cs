using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement inventoryPanel;
    private VisualElement interactionPrompt;
    private Label interactionLabel;

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

        // Attempt initial setup
        InitializeUI();

        // Subscribe to KeyboardInputManager events
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnInventoryToggle += ToggleInventory;
        }
    }

    private void OnDisable()
    {
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnInventoryToggle -= ToggleInventory;
        }
    }

    private void InitializeUI()
    {
        if (uiDocument == null) return;
        
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        inventoryPanel = root.Q<VisualElement>("InventoryPanel");
        interactionPrompt = root.Q<VisualElement>("InteractionPrompt");
        interactionLabel = interactionPrompt?.Q<Label>("InteractionLabel");

        // Set initial visibility state
        if (inventoryPanel != null) 
            inventoryPanel.style.display = DisplayStyle.None;
        
        if (interactionPrompt != null) 
            interactionPrompt.style.display = DisplayStyle.None;
    }

    public void ToggleInventory()
    {
        // Lazy initialize if necessary (e.g., if root was null during OnEnable)
        if (inventoryPanel == null) InitializeUI();
        if (inventoryPanel == null || KeyboardInputManager.Instance == null) return;

        bool isOpen = KeyboardInputManager.Instance.IsInventoryOpen;
        inventoryPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        
        // Handle cursor lock state
        if (isOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else if (!KeyboardInputManager.Instance.IsAnyPanelOpen)
        {
            // Only lock if NO other panels (Escape, Book, etc.) are open
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    public void ShowInteractionPrompt(string text)
    {
        // Lazy initialize if necessary
        if (interactionPrompt == null) InitializeUI();
        if (interactionPrompt == null) return;

        if (interactionLabel != null)
        {
            interactionLabel.text = text;
        }
        
        interactionPrompt.style.display = DisplayStyle.Flex;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPrompt == null) return;
        interactionPrompt.style.display = DisplayStyle.None;
    }
}
