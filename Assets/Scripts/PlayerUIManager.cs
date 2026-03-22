using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private VisualElement inventoryPanel;
    private VisualElement interactionPrompt;
    private Label interactionLabel;

    // Inventory Grid
    private VisualElement inventoryGrid;
    private List<VisualElement> inventorySlots = new List<VisualElement>();

    // Track inventory to update only when changed
    private int lastItemCount = -1;
    private List<InventoryItem> lastItems = new List<InventoryItem>();

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

        // Subscribe to InventoryManager events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
        }

        // Subscribe to KeyboardInputManager events
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnInventoryToggle += ToggleInventory;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
        }

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

        // Find existing UI elements
        inventoryPanel = root.Q<VisualElement>("InventoryPanel");
        interactionPrompt = root.Q<VisualElement>("InteractionPrompt");
        interactionLabel = interactionPrompt?.Q<Label>("InteractionLabel");

        // Find or create inventory grid
        if (inventoryPanel != null)
        {
            inventoryGrid = inventoryPanel.Q<VisualElement>("InventoryGrid");
            if (inventoryGrid == null)
            {
                // Create grid container if not found
                inventoryGrid = new VisualElement();
                inventoryGrid.name = "InventoryGrid";
                inventoryGrid.AddToClassList("inventory-grid");

                // Add to inventory panel
                var scrollView = inventoryPanel.Q<ScrollView>();
                if (scrollView != null)
                {
                    scrollView.Add(inventoryGrid);
                }
                else
                {
                    inventoryPanel.Add(inventoryGrid);
                }
            }
        }

        // Set initial visibility state
        if (inventoryPanel != null)
            inventoryPanel.style.display = DisplayStyle.None;

        if (interactionPrompt != null)
            interactionPrompt.style.display = DisplayStyle.None;

        // Initial inventory update
        RefreshInventoryUI();
    }

    private void RefreshInventoryUI()
    {
        if (InventoryManager.Instance == null) return;
        if (inventoryGrid == null) return;

        List<InventoryItem> currentItems = InventoryManager.Instance.items;

        // Check if inventory changed
        bool inventoryChanged = false;
        if (currentItems.Count != lastItemCount)
        {
            inventoryChanged = true;
        }
        else
        {
            for (int i = 0; i < currentItems.Count; i++)
            {
                if (i >= lastItems.Count ||
                    currentItems[i].itemName != lastItems[i].itemName ||
                    currentItems[i].amount != lastItems[i].amount ||
                    currentItems[i].itemType != lastItems[i].itemType)
                {
                    inventoryChanged = true;
                    break;
                }
            }
        }

        if (inventoryChanged)
        {
            RebuildInventoryGrid(currentItems);
            lastItemCount = currentItems.Count;
            lastItems = new List<InventoryItem>(currentItems);
        }
    }

    private void RebuildInventoryGrid(List<InventoryItem> items)
    {
        if (inventoryGrid == null) return;

        // Clear existing slots
        inventoryGrid.Clear();
        inventorySlots.Clear();

        // Get max slots (based on inventory size)
        int maxSlots = InventoryManager.Instance != null ? InventoryManager.Instance.inventorySize : 20;

        // Create slots for all items
        for (int i = 0; i < maxSlots; i++)
        {
            VisualElement slot = CreateInventorySlot();

            // If we have an item for this slot
            if (i < items.Count && items[i] != null)
            {
                UpdateSlotWithItem(slot, items[i]);
            }
            else
            {
                // Empty slot
                UpdateSlotAsEmpty(slot);
            }

            inventoryGrid.Add(slot);
            inventorySlots.Add(slot);
        }
    }

    private VisualElement CreateInventorySlot()
    {
        var slot = new VisualElement();
        slot.AddToClassList("inventory-slot");

        // Set styles using proper UI Toolkit properties
        slot.style.width = 80;
        slot.style.height = 80;
        slot.style.marginLeft = 5;
        slot.style.marginRight = 5;
        slot.style.marginTop = 5;
        slot.style.marginBottom = 5;
        slot.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        slot.style.borderTopLeftRadius = 5;
        slot.style.borderTopRightRadius = 5;
        slot.style.borderBottomLeftRadius = 5;
        slot.style.borderBottomRightRadius = 5;
        slot.style.borderTopWidth = 1;
        slot.style.borderRightWidth = 1;
        slot.style.borderBottomWidth = 1;
        slot.style.borderLeftWidth = 1;
        slot.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
        slot.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
        slot.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
        slot.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
        slot.style.alignItems = Align.Center;
        slot.style.justifyContent = Justify.Center;
        slot.style.position = Position.Relative;

        // Item icon
        var icon = new VisualElement();
        icon.name = "ItemIcon";
        icon.style.width = 50;
        icon.style.height = 50;
        icon.style.marginBottom = 5;
        icon.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        icon.style.borderTopLeftRadius = 5;
        icon.style.borderTopRightRadius = 5;
        icon.style.borderBottomLeftRadius = 5;
        icon.style.borderBottomRightRadius = 5;
        slot.Add(icon);

        // Item amount label
        var amountLabel = new Label();
        amountLabel.name = "AmountLabel";
        amountLabel.style.position = Position.Absolute;
        amountLabel.style.bottom = 5;
        amountLabel.style.right = 5;
        amountLabel.style.fontSize = 12;
        amountLabel.style.color = Color.white;
        amountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        slot.Add(amountLabel);

        // Item name tooltip (optional)
        var nameLabel = new Label();
        nameLabel.name = "NameLabel";
        nameLabel.style.position = Position.Absolute;
        nameLabel.style.top = 5;
        nameLabel.style.left = 5;
        nameLabel.style.fontSize = 10;
        nameLabel.style.color = Color.white;
        nameLabel.style.display = DisplayStyle.None;
        slot.Add(nameLabel);

        // Add hover effect
        slot.RegisterCallback<MouseEnterEvent>(evt => {
            slot.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            nameLabel.style.display = DisplayStyle.Flex;
        });

        slot.RegisterCallback<MouseLeaveEvent>(evt => {
            slot.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            nameLabel.style.display = DisplayStyle.None;
        });

        return slot;
    }

    private void UpdateSlotWithItem(VisualElement slot, InventoryItem item)
    {
        // Get icon
        var icon = slot.Q<VisualElement>("ItemIcon");
        var amountLabel = slot.Q<Label>("AmountLabel");
        var nameLabel = slot.Q<Label>("NameLabel");

        // Set icon color based on item type
        if (icon != null)
        {
            Color iconColor = GetItemColor(item.itemType, item.itemName);
            icon.style.backgroundColor = iconColor;

            // Optional: Add icon image if you have sprites
            if (item.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(item.icon);
                icon.style.backgroundColor = StyleKeyword.Null;
            }
        }

        // Set amount
        if (amountLabel != null)
        {
            amountLabel.text = item.amount.ToString();
            amountLabel.style.display = DisplayStyle.Flex;
        }

        // Set name for tooltip
        if (nameLabel != null)
        {
            nameLabel.text = item.itemName;
        }

        // Add tooltip attribute
        slot.tooltip = $"{item.itemName}\nType: {item.itemType}\nAmount: {item.amount}";
    }

    private void UpdateSlotAsEmpty(VisualElement slot)
    {
        var icon = slot.Q<VisualElement>("ItemIcon");
        var amountLabel = slot.Q<Label>("AmountLabel");
        var nameLabel = slot.Q<Label>("NameLabel");

        if (icon != null)
        {
            icon.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            icon.style.backgroundImage = StyleKeyword.Null;
        }

        if (amountLabel != null)
        {
            amountLabel.text = "";
            amountLabel.style.display = DisplayStyle.None;
        }

        if (nameLabel != null)
        {
            nameLabel.text = "";
        }

        slot.tooltip = "Empty Slot";
    }

    private Color GetItemColor(ItemType itemType, string itemName)
    {
        switch (itemType)
        {
            case ItemType.Gold:
                return new Color(1f, 0.84f, 0f); // Gold
            case ItemType.Water:
                return new Color(0f, 0.75f, 1f); // Light Blue
            case ItemType.Seed:
                return new Color(0.4f, 0.8f, 0.4f); // Green
            case ItemType.Tool:
                return new Color(0.7f, 0.7f, 0.7f); // Gray
            case ItemType.Food:
                return new Color(1f, 0.5f, 0.3f); // Orange
            case ItemType.Material:
                return new Color(0.6f, 0.4f, 0.2f); // Brown
            default:
                return new Color(0.5f, 0.5f, 0.5f); // Gray
        }
    }

    public void ToggleInventory()
    {
        // Lazy initialize if necessary
        if (inventoryPanel == null) InitializeUI();
        if (inventoryPanel == null || KeyboardInputManager.Instance == null) return;

        bool isOpen = KeyboardInputManager.Instance.IsInventoryOpen;
        inventoryPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // Refresh inventory display when opening
        if (isOpen)
        {
            RefreshInventoryUI();
        }

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

    // Optional: Force refresh inventory UI
    public void ForceRefreshInventory()
    {
        RefreshInventoryUI();
    }

}
