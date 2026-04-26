using UnityEngine;
using System;
using System.Collections.Generic;

// See Docs/CraftingRecipe.md for detailed description and usage notes.

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
