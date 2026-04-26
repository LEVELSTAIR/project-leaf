using UnityEngine;
using System;

// See Docs/InventoryItem.md for details and usage tips.]
[System.Serializable]
public class InventoryItem
{
    public ItemType itemType;
    public string itemName;
    public int amount;
    public Sprite icon;
    public int maxStackSize = 99;
    // Note: `maxStackSize` determines how many units of this item can
    // occupy a single inventory slot. `icon` is optional and used for
    // UI representations (hotbar, inventory slots, recipe lists).

    public InventoryItem(ItemType type, string name, int amount, Sprite icon = null)
    {
        this.itemType = type;
        this.itemName = name;
        this.amount = amount;
        this.icon = icon;
        // Constructed InventoryItem instances are simple DTOs; modify
        // `maxStackSize` on the instance if you need per-item stack tuning.
    }

    public bool CanStack(InventoryItem other)
    {
        return other != null &&
               other.itemType == this.itemType &&
               other.itemName == this.itemName;
    }
}

public enum ItemType
{
    Seed,
    Water,
    Gold,
    Tool,
    Food,
    Wood,
    Material
}
