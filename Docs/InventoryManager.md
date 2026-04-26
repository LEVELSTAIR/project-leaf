InventoryManager

Centralized runtime inventory system used by gameplay systems and UI.

Responsibilities:
- Track item stacks and special resource counters (gold, water, clay, wood).
- Provide APIs to add / remove / query items (`AddItem`, `RemoveItem`, `GetItemAmount`, `HasItem`).
- Maintain a simple slot-based list (`items`) and separate fast-access counters for common resources used by other systems (HUD, crafting).
- Emit events (`OnItemAdded`, `OnItemRemoved`, `OnInventoryChanged`) so UI and game logic can react to inventory changes.

Debug / Test support:
- The inspector exposes a small "Debug - Test Data" section that can auto-populate the inventory on Start() for rapid testing.

Notes / tips:
- Keep `itemName` strings consistent with crafting/consumption logic to avoid mismatches when checking or removing resources.
- This manager is intentionally lightweight; consider replacing the internal representation with a more advanced container if you need features like unique IDs, durability, or equipment slots.
