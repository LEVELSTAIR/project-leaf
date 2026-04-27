using UnityEngine;
using System;

<<<<<<< HEAD
// See Docs/InventoryItem.md for details and usage tips.]
=======
/*
 InventoryItem

 Lightweight data container used by the InventoryManager to represent
 a stackable item. This class is intentionally serializable so it can
 be stored in lists, shown in the inspector during debugging, and
 passed around game systems.

 Key responsibilities:
 - Hold identifying data (`itemName`, `itemType`) and runtime quantity
   (`amount`).
 - Carry an optional `icon` for UI display and `maxStackSize` to
   control stacking behavior.
 - Provide a small helper (`CanStack`) to check stack compatibility
   between two items.

 Usage notes:
 - Keep `itemName` values consistent with crafting and other
   resource-checking systems to avoid mismatches.
 - `maxStackSize` can be tuned per item after construction if needed.
 */

>>>>>>> 3039158 (Cage crafting)
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
