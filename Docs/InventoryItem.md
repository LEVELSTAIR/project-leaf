InventoryItem

Lightweight data container used by the InventoryManager to represent a stackable item. This class is intentionally serializable so it can be stored in lists, shown in the inspector during debugging, and passed around game systems.

Key responsibilities:
- Hold identifying data (`itemName`, `itemType`) and runtime quantity (`amount`).
- Carry an optional `icon` for UI display and `maxStackSize` to control stacking behavior.
- Provide a small helper (`CanStack`) to check stack compatibility between two items.

Usage notes:
- Keep `itemName` values consistent with crafting and other resource-checking systems to avoid mismatches.
- `maxStackSize` can be tuned per item after construction if needed.
