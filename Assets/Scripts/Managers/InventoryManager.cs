using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    public int inventorySize = 20;
    public List<InventoryItem> items = new List<InventoryItem>();

    [Header("UI Reference")]
    public HUDManager hudManager;

    [Header("Item max limit")]

    [Header("Max Stack Sizes")]
    public int maxGoldStack = 999;
    public int maxWaterStack = 999;
    public int maxSeedStack = 99;
    public int defaultMaxStack = 99;


    // Events for UI updates
    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action OnInventoryChanged;

    // Separate tracking for different item types
    private int totalGold;
    private int totalWater;
    private Dictionary<string, int> seeds = new Dictionary<string, int>();
    private int totalClay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        items = new List<InventoryItem>(inventorySize);
    }

    private void Start()
    {
        if (hudManager == null)
        {
            hudManager = HUDManager.Instance;
        }
    }

    /// <summary>
    /// Add item to inventory
    /// </summary>
    public bool AddItem(string itemName, ItemType itemType, int amount, Sprite icon = null)
    {
        if (amount <= 0) return false;

        // Handle special items separately for quick access
        switch (itemType)
        {
            case ItemType.Gold:
                totalGold += amount;
                break;
            case ItemType.Water:
                totalWater += amount;
                break;
            case ItemType.Seed:
                if (seeds.ContainsKey(itemName))
                    seeds[itemName] += amount;
                else
                    seeds[itemName] = amount;
                break;
            case ItemType.Material:
                if (itemName.ToLower() == "clay")
                    totalClay += amount;
                break;
        }

        int remainingAmount = amount;

        // Try to stack with existing items
        InventoryItem existingItem = items.Find(i => i.itemName == itemName && i.itemType == itemType);

        if (existingItem != null)
        {
            int spaceLeft = existingItem.maxStackSize - existingItem.amount;
            if (spaceLeft >= remainingAmount)
            {
                existingItem.amount += remainingAmount;
                OnItemAdded?.Invoke(existingItem);
                OnInventoryChanged?.Invoke();
                UpdateUI();
                Debug.Log($"Added {amount} {itemName}(s). Total: {existingItem.amount}");
                return true;
            }
            else if (spaceLeft > 0)
            {
                existingItem.amount = existingItem.maxStackSize;
                remainingAmount -= spaceLeft;
                OnItemAdded?.Invoke(existingItem);
            }
        }

        // Add remaining items to new slots
        while (remainingAmount > 0)
        {
            if (items.Count >= inventorySize)
            {
                Debug.LogWarning("Inventory is full!");
                UpdateUI();
                return false;
            }

            int stackAmount = Mathf.Min(remainingAmount, GetMaxStackSize(itemName, itemType));
            InventoryItem newItem = new InventoryItem(itemType, itemName, stackAmount, icon);
            items.Add(newItem);
            remainingAmount -= stackAmount;
            OnItemAdded?.Invoke(newItem);
        }

        OnInventoryChanged?.Invoke();
        UpdateUI();
        Debug.Log($"Added {amount} {itemName}(s) to inventory");
        return true;
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(string itemName, ItemType itemType, int amount)
    {
        if (amount <= 0) return false;

        int totalAvailable = GetItemAmount(itemName, itemType);
        if (totalAvailable < amount) return false;

        // Handle special items
        switch (itemType)
        {
            case ItemType.Gold:
                totalGold -= amount;
                break;
            case ItemType.Water:
                totalWater -= amount;
                break;
            case ItemType.Seed:
                if (seeds.ContainsKey(itemName))
                {
                    seeds[itemName] -= amount;
                    if (seeds[itemName] <= 0)
                        seeds.Remove(itemName);
                }
                break;
            case ItemType.Material:
                if (itemName.ToLower() == "clay")
                {
                    totalClay -= amount;
                    if (totalClay < 0) totalClay = 0;
                }
                break;
        }

        int remainingToRemove = amount;

        // Remove from inventory slots
        for (int i = items.Count - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            InventoryItem item = items[i];
            if (item.itemName == itemName && item.itemType == itemType)
            {
                if (item.amount <= remainingToRemove)
                {
                    remainingToRemove -= item.amount;
                    items.RemoveAt(i);
                    OnItemRemoved?.Invoke(item);
                }
                else
                {
                    item.amount -= remainingToRemove;
                    remainingToRemove = 0;
                    OnItemRemoved?.Invoke(item);
                }
            }
        }

        OnInventoryChanged?.Invoke();
        UpdateUI();
        Debug.Log($"Removed {amount} {itemName}(s). Remaining: {GetItemAmount(itemName, itemType)}");
        return true;
    }

    /// <summary>
    /// Get total amount of a specific item
    /// </summary>
    public int GetItemAmount(string itemName, ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Gold:
                return totalGold;
            case ItemType.Water:
                return totalWater;
            case ItemType.Seed:
                return seeds.ContainsKey(itemName) ? seeds[itemName] : 0;
            case ItemType.Material:
                if (itemName.ToLower() == "clay")
                    return totalClay;
                break;
            default:
                return items.Where(i => i.itemName == itemName && i.itemType == itemType)
                           .Sum(i => i.amount);
        }

        return 0; // Added missing return path
    }

    /// <summary>
    /// Check if player has enough of an item
    /// </summary>
    public bool HasItem(string itemName, ItemType itemType, int requiredAmount)
    {
        return GetItemAmount(itemName, itemType) >= requiredAmount;
    }

    private int GetMaxStackSize(string itemName, ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Gold:
                return maxGoldStack;
            case ItemType.Water:
                return maxWaterStack;
            case ItemType.Seed:
                return maxSeedStack;
            default:
                return defaultMaxStack;
        }
    }

    private void UpdateUI()
    {
        if (hudManager != null)
        {
            hudManager.UpdateGold(totalGold);
            hudManager.UpdateWater(totalWater);
            hudManager.UpdateClay(totalClay);
            hudManager.UpdateSeeds(seeds);
        }
    }

    public int GetTotalGold()
    {
        return totalGold;
    }

    public int GetTotalWater()
    {
        return totalWater;
    }

    public int GetTotalClay() // Clay getter
    {
        return totalClay;
    }

    public Dictionary<string, int> GetAllSeeds()
    {
        return new Dictionary<string, int>(seeds);
    }

    public void ClearInventory()
    {
        items.Clear();
        totalGold = 0;
        totalWater = 0;
        totalClay = 0; // Ensure totalClay is reset
        seeds.Clear();
        OnInventoryChanged?.Invoke();
        UpdateUI();
    }
}