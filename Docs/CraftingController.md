CraftingController

A centralized crafting controller responsible for runtime behavior of the crafting system.

Responsibilities:
- Accepts crafting requests (`TryCraftRecipe`) and verifies resource availability via `CraftingRecipe.CanCraft`.
- Consumes resources and starts a placement flow for the crafted object (`StartPlacement` / `HandlePlacement`).
- Instantiates the preview object, removes its colliders and applies preview materials (valid/invalid) for visual feedback.
- Positions the preview using a camera-forward raycast (configurable camera) and falls back to a point in front of the camera when the raycast misses.
- Confirms placement, instantiating the final object and optionally parenting it under the recipe's `parentTransform` for scene organization.
- Provides debugging aids: placement gizmos and detailed log messages.
- Integrates with other systems:
    * `InventoryManager` for resource checks/consumption
    * `CraftingUIManager` to open/close crafting UI
    * `KeyboardInputManager` to coordinate cursor state
    * `HUDManager` to display placement messages and feedback

Usage notes / tips:
- Assign `placementCamera` in the inspector to control which camera is used for placement rays and gizmo visualization. If left empty, the controller falls back to `Camera.main`.
- Configure `placementMask` and `placementMaxDistance` to control where items can be placed in the world.
- Provide `validPlacementMaterial` and `invalidPlacementMaterial` to clearly indicate valid/invalid placement areas.
- Use `parentTransform` on each `CraftingRecipe` asset to keep spawned objects organized under a dedicated GameObject in the scene.
- Toggle `showPlacementGizmos` during development to visualize the placement ray and target point in the Scene view.

This controller is intentionally data-driven: most per-recipe configuration lives in `CraftingRecipe` ScriptableObjects so new craftables can be defined without code changes.
