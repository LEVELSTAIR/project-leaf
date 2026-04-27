using UnityEngine;
using System.Collections.Generic;
using System.Linq;

<<<<<<< HEAD
<<<<<<< HEAD
// See Docs/InventoryManager.md for detailed description and usage notes.
=======
/*
 InventoryManager

 Centralized runtime inventory system used by gameplay systems and UI.

 Responsibilities:
 - Track item stacks and special resource counters (gold, water, clay, wood).
 - Provide APIs to add / remove / query items (`AddItem`, `RemoveItem`,
   `GetItemAmount`, `HasItem`).
 - Maintain a simple slot-based list (`items`) and separate fast-access
   counters for common resources used by other systems (HUD, crafting).
 - Emit events (`OnItemAdded`, `OnItemRemoved`, `OnInventoryChanged`) so
   UI and game logic can react to inventory changes.

 Debug / Test support:
 - The inspector exposes a small "Debug - Test Data" section that can
   auto-populate the inventory on Start() for rapid testing.

 Notes / tips:
 - Keep `itemName` strings consistent with crafting/consumption logic to
   avoid mismatches when checking or removing resources.
 - This manager is intentionally lightweight; consider replacing the
   internal representation with a more advanced container if you need
   features like unique IDs, durability, or equipment slots.
 */
>>>>>>> 3039158 (Cage crafting)
=======
// See Docs/InventoryManager.md for detailed description and usage notes.
>>>>>>> cbe9f10 (Descriptions added)

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    public int inventorySize = 20;
    public List<InventoryItem> items = new List<InventoryItem>();

    [Header("UI Reference")]
    public HUDManager hudManager;

    [Header("Item max limit")]

    // Maximum stack sizes for various item categories. These can be tuned
    // in the inspector to change how many units fit in a single inventory
    // slot for each resource type.
    [Header("Max Stack Sizes")]
    public int maxGoldStack = 999;
    public int maxWaterStack = 999;
    public int maxSeedStack = 99;
    public int defaultMaxStack = 99;

    [Header("Debug - Test Data")]
    public bool autoFillTestData = false;
    public int testGoldAmount = 500;
    public int testWaterAmount = 300;
    public int testClayAmount = 200;
    public int testWoodAmount = 150;
    public Dictionary<string, int> testSeeds = new Dictionary<string, int>();


    // Events for UI updates and other listeners. Subscribe to these to be
    // notified when items are added/removed or the inventory changes.
    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action OnInventoryChanged;

    // Fast-access counters for commonly referenced resource types. These
    // are maintained alongside the slot list for quick queries and UI
    // updates (e.g. HUD). Keep them in sync with AddItem/RemoveItem.
    private int totalGold;
    private int totalWater;
    private Dictionary<string, int> seeds = new Dictionary<string, int>();
    private int totalClay;
    private int totalWood;

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

        // Auto-fill test data if enabled
        if (autoFillTestData)
        {
            PopulateTestInventory();
        }
    }

    /// <summary>
    /// Add item to inventory
    /// </summary>
    public bool AddItem(string itemName, ItemType itemType, int amount, Sprite icon = null)
    {
        // Validate input early.
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
                Debug.Log($"[InventoryManager] Seed stored: '{itemName}' = {seeds[itemName]}");
                break;
            case ItemType.Material:
                if (itemName.ToLower() == "clay")
                    totalClay += amount;
                break;
            case ItemType.Wood:
                if (itemName.ToLower() == "wood")
                    totalWood += amount;
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
        // Removes up to `amount` from the inventory and updates fast-tracked
        // counters. Returns true when removal succeeded.
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
            case ItemType.Wood:
                if (itemName.ToLower() == "wood")
                {
                    totalWood -= amount;
                    if (totalWood < 0) totalWood = 0;
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
        // Returns the total amount of the given item available in inventory.
        // Uses fast counters for special resources to avoid iterating slots.
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
            case ItemType.Wood:
                if (itemName.ToLower() == "wood")
                    return totalWood;
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

    /// <summary>
    /// Debug method to log all seeds in inventory
    /// </summary>
    public void DebugLogAllSeeds()
    {
        Debug.Log("[InventoryManager] === SEED INVENTORY DEBUG ===");
        if (seeds.Count == 0)
        {
            Debug.Log("[InventoryManager] No seeds in inventory!");
        }
        else
        {
            foreach (var kvp in seeds)
            {
                Debug.Log($"[InventoryManager] Seed: '{kvp.Key}' Count: {kvp.Value}");
            }
        }
        Debug.Log("[InventoryManager] === END DEBUG ===");
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

    /// <summary>
    /// Populates inventory with test amounts of resources for debugging/testing.
    /// Configure amounts in the Inspector under "Debug - Test Data" section.
    /// </summary>
    public void PopulateTestInventory()
    {
        // Add configured debug/test resources. This is helpful during
        // development to quickly populate the player with materials.
        Debug.Log("[InventoryManager] Populating test inventory...");

        // Add test gold
        if (testGoldAmount > 0)
        {
            AddItem("Gold", ItemType.Gold, testGoldAmount);
            Debug.Log($"[InventoryManager] Added {testGoldAmount} Gold");
        }

        // Add test water
        if (testWaterAmount > 0)
        {
            AddItem("Water", ItemType.Water, testWaterAmount);
            Debug.Log($"[InventoryManager] Added {testWaterAmount} Water");
        }

        // Add test clay
        if (testClayAmount > 0)
        {
            AddItem("Clay", ItemType.Material, testClayAmount);
            Debug.Log($"[InventoryManager] Added {testClayAmount} Clay");
        }

        // Add test wood
        if (testWoodAmount > 0)
        {
            AddItem("Wood", ItemType.Wood, testWoodAmount);
            Debug.Log($"[InventoryManager] Added {testWoodAmount} Wood");
        }

        // Add test seeds from dictionary
        if (testSeeds != null && testSeeds.Count > 0)
        {
            foreach (var seedEntry in testSeeds)
            {
                if (seedEntry.Value > 0)
                {
                    AddItem(seedEntry.Key, ItemType.Seed, seedEntry.Value);
                    Debug.Log($"[InventoryManager] Added {seedEntry.Value} {seedEntry.Key} seeds");
                }
            }
        }

        Debug.Log("[InventoryManager] Test inventory populated!");
    }

}
