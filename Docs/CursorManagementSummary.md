# Mouse Cursor Management - How It's Implemented

## Summary

The mouse cursor now properly appears and disappears for the flower seed planting system by integrating with the existing **KeyboardInputManager** cursor management system.

## What Was Changed

### The Problem
- Mouse wasn't appearing when the flower planting UI opened
- Cursor state wasn't being synchronized between systems

### The Solution
Three files were updated to properly manage cursor visibility:

## 1. FlowerSeedPlantingUIManager.cs

**Key Changes:**
- Added cursor management to `SetUIVisible()` method
- When UI opens: **Cursor is shown** (unlocked, visible)
- When UI closes: **Call SyncCursorState()** to let KeyboardInputManager decide

```csharp
public void SetUIVisible(bool visible)
{
    // Show/hide UI panel

    if (visible)
    {
        // SHOW CURSOR
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
    else
    {
        // LET KEYBOARDINPUTMANAGER DECIDE
        KeyboardInputManager.Instance?.SyncCursorState();
    }
}
```

- Updated `OnCloseButtonClicked()` to sync cursor state
- Added `IsUIVisible()` property for state tracking

## 2. FlowerSeedPlantingController.cs

**Key Changes:**
- Updated `ConfirmPlanting()` to sync cursor after placing
- Updated `CancelPlanting()` to sync cursor after canceling

```csharp
// After planting or canceling
FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

if (KeyboardInputManager.Instance != null)
{
    KeyboardInputManager.Instance.SyncCursorState();
}
```

## How KeyboardInputManager Controls Cursor

The `KeyboardInputManager` centrally manages cursor state for all UI panels:

```csharp
private void UpdateCursorState()
{
    bool isCurrentlyPlacing = CraftingController.Instance != null && 
                              CraftingController.Instance.IsCurrentlyPlacing();
    bool shouldShowCursor = IsAnyPanelOpen && !isCurrentlyPlacing;

    UnityEngine.Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;
    UnityEngine.Cursor.visible = shouldShowCursor;
}
```

**Logic:**
- If ANY panel is open (Inventory, Craft, Planting, etc.) ? Cursor visible
- If NO panels open AND not placing ? Cursor locked/hidden
- If currently placing ? Cursor locked/hidden (even if panel open)

## Cursor Visibility States

### ? Cursor VISIBLE
- Planting UI is open
- Inventory is open
- Crafting UI is open
- Any other panel is open
- Any menu is displayed

### ? Cursor HIDDEN (Locked)
- All panels are closed
- Player is in first-person gameplay mode
- During placement mode (even if UI visible)

## Event Flow

```
[Player presses F near soil]
        ?
[SoilController.Interact()]
        ?
[ShowFlowerSeedUI()]
        ?
[FlowerSeedPlantingUIManager.ShowUI()]
        ?
[SetUIVisible(true)]
        ?? Panel appears
        ?? Cursor.lockState = None
        ?? Cursor.visible = true  ? MOUSE APPEARS HERE
        ?? UI refreshed

[Player selects flower]
        ?
[FlowerSeedPlantingController.StartPlantingMode()]
        ?? Placement preview shown
        ?? Cursor hidden (placement mode)

[Player clicks to place]
        ?
[ConfirmPlanting()]
        ?? Flower spawned
        ?? UI closed
        ?? SyncCursorState() called
           ?? If no other panels ? Cursor hidden
           ?? If other panels open ? Cursor visible
```

## Integration with Crafting System

The planting system reuses the same cursor management as crafting:

**Crafting:**
- `CraftingController.IsCurrentlyPlacing()` checks if placing
- Placement mode hides cursor automatically
- UI closing triggers cursor sync

**Planting:**
- Same pattern implemented
- Uses same `KeyboardInputManager` system
- Plays nicely with other UI systems

## Why This Works

1. **Centralized Management** - One system (`KeyboardInputManager`) controls all cursor states
2. **Panel Tracking** - Tracks which panels are open via boolean flags
3. **Consistent Behavior** - All UI panels behave the same way
4. **Non-Breaking** - Doesn't interfere with crafting or other systems
5. **Efficient** - Only updates cursor when state changes

## Testing

To verify cursor management is working:

1. ? Start game ? Cursor is hidden (gameplay mode)
2. ? Interact with soil ? Cursor appears
3. ? Click X button ? Cursor disappears
4. ? Interact with soil ? Cursor appears again
5. ? Open Inventory too ? Cursor stays visible
6. ? Close Inventory ? Cursor stays visible (planting UI still open)
7. ? Close planting UI ? Cursor disappears
8. ? Open Inventory ? Cursor appears
9. ? Close Inventory ? Cursor disappears

## Files Modified

| File | Changes |
|------|---------|
| `FlowerSeedPlantingUIManager.cs` | Cursor management in SetUIVisible(), close button handler |
| `FlowerSeedPlantingController.cs` | Cursor sync in ConfirmPlanting() and CancelPlanting() |

## Build Status

? **Build Successful** - All changes compile without errors
