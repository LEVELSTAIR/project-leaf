using UnityEngine;
using System;
using System.Collections.Generic;

<<<<<<< HEAD
<<<<<<< HEAD
// See Docs/CraftingRecipe.md for detailed description and usage notes.
=======
/*
 A versatile crafting data component.

 This ScriptableObject defines a single crafting recipe and contains all
 data required by the crafting system:

 - `recipeName` : Human readable name shown in UI.
 - `icon`       : Optional sprite used for recipe lists.
 - `resultPrefab`: Prefab spawned when the recipe is placed/confirmed.
 - `parentTransform`: Optional scene Transform to parent instantiated
                      results under (keeps hierarchy organized).
 - `requiredResources`: List of `ResourceRequirement` entries describing
                        the item names, types and counts needed to craft.

 Utility methods:
 - `CanCraft(InventoryManager)` checks the provided inventory for
   sufficient resources.
 - `ConsumeResources(InventoryManager)` removes the required resources
   from the provided inventory when crafting is started/confirmed.

 Notes and tips:
 - Create different `CraftingRecipe` assets (Assets -> Create -> Crafting/Recipe)
   to represent any craftable object (pots, tools, buildings, etc.).
 - Assign a `parentTransform` if you want all spawned objects to be
   grouped under a specific scene GameObject (useful for cleanup and
   inspector organization).
 - Keep `itemName` values consistent with the Inventory system to avoid
   mismatches when checking/consuming resources.
 - ScriptableObject data is read-only at runtime by convention; modify
   assets in the editor when creating or tuning recipes.
 */
>>>>>>> 3039158 (Cage crafting)
=======
// See Docs/CraftingRecipe.md for detailed description and usage notes.
>>>>>>> cbe9f10 (Descriptions added)

[Serializable]
public class ResourceRequirement
{
    public string itemName;
    public ItemType itemType;
    public int amount;
}

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Sprite icon;
    public GameObject resultPrefab; // The pot prefab to spawn          
    public Transform parentTransform; // Parent object for spawned items By adding to parent item it will be much easier to handle the game objects in the inspector
    public List<ResourceRequirement> requiredResources = new List<ResourceRequirement>();

    [TextArea(2, 4)]
    public string description;

    // Helper to check if player can craft this
    public bool CanCraft(InventoryManager inventory)
    {
        foreach (var req in requiredResources)
        {
            if (!inventory.HasItem(req.itemName, req.itemType, req.amount))
                return false;
        }
        return true;
    }

    // Helper to consume resources
    public void ConsumeResources(InventoryManager inventory)
    {
        foreach (var req in requiredResources)
        {
            inventory.RemoveItem(req.itemName, req.itemType, req.amount);
        }
    }
}
