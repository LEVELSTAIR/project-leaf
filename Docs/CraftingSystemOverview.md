Crafting System Overview

This document explains how the crafting pieces in the project work together to produce the final placed object in the scene. It describes responsibilities, data flow, integration points, and common troubleshooting steps.

1. High-level Flow

- Designer creates a `CraftingRecipe` asset describing the recipe (result prefab, required resources, optional `parentTransform`).
- Player opens the Crafting UI (`CraftingUIManager`) and clicks a recipe's Craft button.
- `CraftingUIManager` closes the UI and tells `CraftingController` to begin crafting that recipe.
- `CraftingController` verifies resources via `CraftingRecipe.CanCraft` and consumes them (`ConsumeResources`).
- A preview of the result prefab is instantiated and a placement flow begins.
- The preview is positioned using a camera-forward raycast (camera configurable) and visually marked valid/invalid using the configured preview materials.
- Player confirms placement (left click) or cancels (X / right click). On confirm the final object is instantiated, optionally parented under the recipe's `parentTransform`.
- `InventoryManager` has been updated earlier (resources consumed) and HUD feedback is shown via `HUDManager`.

2. Core Components

- `CraftingRecipe` (ScriptableObject)
  - Data only: `recipeName`, `icon`, `resultPrefab`, `parentTransform`, `requiredResources`.
  - Helpers: `CanCraft(InventoryManager)` and `ConsumeResources(InventoryManager)`.

- `CraftingUIManager` (MonoBehaviour)
  - Displays recipe list (UXML). Handles craft button clicks.
  - Closes UI when a recipe is clicked and signals `CraftingController` to start placement.
  - Keeps recipe buttons for UI updates and calls `RefreshUI` when inventory changes.

- `CraftingController` (MonoBehaviour)
  - Runtime logic for crafting and placement.
  - On craft: checks resources, consumes them, instantiates preview, removes colliders, applies preview material.
  - Placement: uses `placementCamera` (or `Camera.main`) to raycast forward, positions preview at hit point or fallback position, updates preview material to valid/invalid.
  - Confirm placement: instantiates final prefab, uses `currentRecipe.parentTransform` if set.
  - Cancel placement: destroys preview and restores UI/cursor state.
  - Debug: gizmos show placement point (orange/blue) and logs renderer/material info.

- `KeyboardInputManager` (MonoBehaviour)
  - Global input state for UI toggles and hotbar.
  - Tracks whether UI panels are open and updates cursor lock/visibility accordingly.
  - Integration: `CraftingUIManager` and `CraftingController` call `SetCraftOpen` and `SyncCursorState` to ensure cursor is in the correct state when entering/exiting placement.

- `InventoryManager` (MonoBehaviour)
  - Tracks item stacks and special resource counters.
  - Methods used by crafting: `HasItem`, `GetItemAmount`, `RemoveItem`, `AddItem` (for testing or seed fallback).
  - Emits `OnInventoryChanged` so UI updates (`CraftingUIManager.RefreshUI`) when resources change.

- `HUDManager` (MonoBehaviour)
  - Displays placement instructions, success/cancel messages, and other temporary messages.
  - `CraftingController` calls `ShowMessage` with a long duration during placement and shorter messages on success/cancel.

3. Placement Details

- Camera-based placement: The preview is positioned by casting a ray from the configured camera's position in its forward direction.
- Raycast uses `placementMask` and `placementMaxDistance`.
- If raycast hits, preview snaps to `hit.point` and is considered valid (subject to `IsPlacementValid` checks).
- If raycast misses, preview is placed at a fallback point in front of the camera; preview marked invalid (invalid material).
- Confirm instantiation uses `Instantiate(prefab, position, rotation, parent)` when recipe has `parentTransform`.

4. How to add a new craftable

1. Create a `CraftingRecipe` asset: Assets -> Create -> Crafting/Recipe.
2. Set `recipeName` and `icon` (optional).
3. Assign `resultPrefab` (the object to place).
4. Optionally assign `parentTransform` by dragging a GameObject from the scene into the asset field.
5. Add `requiredResources` entries to match `InventoryManager` item names and types.
6. Add the recipe asset to `CraftingUIManager.recipesToDisplay` (Inspector) and run.

5. Debugging and Common Issues

- Preview not visible:
  - Check prefab has Renderer components.
  - Check `validPlacementMaterial` / `invalidPlacementMaterial` assigned and shader is compatible.
  - Ensure preview is not positioned far away (look at debug logs for preview position).

- Cursor / movement locked during placement:
  - System relies on `KeyboardInputManager`'s panel state and `CraftingController.IsCurrentlyPlacing()`.
  - `CraftingUIManager` sets craft state when opening/closing; `CraftingController` calls `SetCraftOpen(false)` when placement starts and ends. If cursor stays locked, ensure `KeyboardInputManager.SetCraftOpen` is called or call `KeyboardInputManager.SyncCursorState()` after state changes.

- Gizmo not showing ray line:
  - Assign `placementCamera` in `CraftingController` inspector or rely on `Camera.main`.
  - Toggle `showPlacementGizmos`.

6. Testing Tips

- Use `InventoryManager` debug fields (autoFillTestData) to seed resources quickly.
- Toggle `showPlacementGizmos` and use Scene view to inspect preview position and ray.
- Confirm `parentTransform` parenting by inspecting hierarchy after placement.

7. Extensions

- `IsPlacementValid` can be extended to check overlaps, terrain slope, proximity to other objects, or player-owned areas.
- Add RPC/network hooks to replicate placement in multiplayer scenarios.


---

For more details see the per-class docs in the `Docs/` directory: `CraftingRecipe.md`, `CraftingController.md`, `InventoryManager.md`, `InventoryItem.md`.
