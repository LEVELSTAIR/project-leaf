using UnityEngine;
using System;

[System.Serializable]
public class InventoryItem
{
    public ItemType itemType;
    public string itemName;
    public int amount;
    public Sprite icon;
    public int maxStackSize = 99;

    public InventoryItem(ItemType type, string name, int amount, Sprite icon = null)
    {
        this.itemType = type;
        this.itemName = name;
        this.amount = amount;
        this.icon = icon;
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
    Material
}
