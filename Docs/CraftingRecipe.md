CraftingRecipe

This ScriptableObject defines a single crafting recipe and contains all data required by the crafting system:

- `recipeName` : Human readable name shown in UI.
- `icon`       : Optional sprite used for recipe lists.
- `resultPrefab`: Prefab spawned when the recipe is placed/confirmed.
- `parentTransform`: Optional scene Transform to parent instantiated results under (keeps hierarchy organized).
- `requiredResources`: List of `ResourceRequirement` entries describing the item names, types and counts needed to craft.

Utility methods:
- `CanCraft(InventoryManager)` checks the provided inventory for sufficient resources.
- `ConsumeResources(InventoryManager)` removes the required resources from the provided inventory when crafting is started/confirmed.

Notes and tips:
- Create different `CraftingRecipe` assets (Assets -> Create -> Crafting/Recipe) to represent any craftable object (pots, tools, buildings, etc.).
- Assign a `parentTransform` if you want all spawned objects to be grouped under a specific scene GameObject (useful for cleanup and inspector organization).
- Keep `itemName` values consistent with the Inventory system to avoid mismatches when checking/consuming resources.
- ScriptableObject data is read-only at runtime by convention; modify assets in the editor when creating or tuning recipes.
