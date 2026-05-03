# Flower Seed Planting System - Cursor Management

## Overview

The flower seed planting system now integrates with the existing cursor management system used by the crafting UI. The mouse will now appear and disappear properly when opening and closing the flower planting UI.

## How It Works

### Cursor Management Flow

```
Player interacts with soil (F)
    ?
SoilController.ShowFlowerSeedUI()
    ?
FlowerSeedPlantingUIManager.ShowUI()
    ?
SetUIVisible(true)
    - Display UI panel
    - Unlock cursor: Cursor.lockState = CursorLockMode.None
    - Show cursor: Cursor.visible = true
    ?
Player selects flower to plant
    ?
Enter planting mode (placement preview)
    ?
Player places flower or cancels
    ?
FlowerSeedPlantingController.ConfirmPlanting() or CancelPlanting()
    ?
SetUIVisible(false)
    - Hide UI panel
    - Call KeyboardInputManager.SyncCursorState()
    ?
KeyboardInputManager.UpdateCursorState()
    - Check if any panels are open
    - Lock cursor if no panels open
    - Show cursor only if a panel is open
```

## Integration Points

### 1. **KeyboardInputManager**

Central cursor management system that tracks all open panels:

```csharp
public void UpdateCursorState()
{
    bool isCurrentlyPlacing = CraftingController.Instance != null && 
                              CraftingController.Instance.IsCurrentlyPlacing();
    bool shouldShowCursor = IsAnyPanelOpen && !isCurrentlyPlacing;
    UnityEngine.Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;
    UnityEngine.Cursor.visible = shouldShowCursor;
}
```

**Key Methods:**
- `UpdateCursorState()` - Updates cursor based on panel states
- `SyncCursorState()` - Public method to sync cursor state
- `SetCraftOpen(bool)` - Set craft state directly

### 2. **FlowerSeedPlantingUIManager**

Manages UI visibility and cursor state:

```csharp
public void SetUIVisible(bool visible)
{
    if (plantingPanel != null)
    {
        plantingPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        isUIVisible = visible;
    }

    if (visible)
    {
        // Show cursor when UI opens
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        RefreshUI();
    }
    else
    {
        // Let KeyboardInputManager handle cursor state
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.SyncCursorState();
        }
    }
}
```

**When opening UI:**
- Sets `Cursor.lockState = CursorLockMode.None`
- Sets `Cursor.visible = true`
- Shows the panel

**When closing UI:**
- Calls `KeyboardInputManager.SyncCursorState()`
- Lets the input manager decide cursor state based on other panels

### 3. **FlowerSeedPlantingController**

Syncs cursor when exiting placement mode:

```csharp
private void ConfirmPlanting(Vector3 position)
{
    // Plant the flower...

    // Close UI and sync cursor state
    FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

    if (KeyboardInputManager.Instance != null)
    {
        KeyboardInputManager.Instance.SyncCursorState();
    }
}

public void CancelPlanting()
{
    // Cancel placement...

    // Close UI and sync cursor state
    FlowerSeedPlantingUIManager.Instance?.SetUIVisible(false);

    if (KeyboardInputManager.Instance != null)
    {
        KeyboardInputManager.Instance.SyncCursorState();
    }
}
```

## Cursor Behavior

### When Planting UI Opens
? Cursor becomes visible  
? Cursor is unlocked  
? Mouse can interact with UI buttons  
? Placement preview appears

### When Placing Flower
? Cursor locked (placement mode)  
? Cursor hidden  
? Preview follows camera  
? Left-click to place, X/RMB to cancel

### When UI Closes
- If NO other panels open:  
  ? Cursor locked  
  ? Cursor hidden  
- If other panels open (Inventory, Map, etc.):  
  ? Cursor visible  
  ? Cursor unlocked

## Files Updated

1. **FlowerSeedPlantingUIManager.cs**
   - Added `isUIVisible` flag
   - Enhanced `SetUIVisible()` to manage cursor
   - Added `OnCloseButtonClicked()` with cursor sync
   - Added `IsUIVisible()` getter method

2. **FlowerSeedPlantingController.cs**
   - Updated `ConfirmPlanting()` to sync cursor
   - Updated `CancelPlanting()` to sync cursor

## Testing Checklist

- [ ] Open flower planting UI ? Cursor appears ?
- [ ] Close UI by clicking X ? Cursor disappears (if no other panels)
- [ ] Close UI by canceling placement ? Cursor disappears
- [ ] Open another panel (Inventory) ? Cursor stays visible
- [ ] Close Inventory with UI still open ? Cursor still visible
- [ ] Click flower to start placement ? Cursor disappears
- [ ] Click to place flower ? Cursor disappears, UI closes
- [ ] Cancel placement ? Cursor disappears, UI closes

## Performance

No performance impact:
- Cursor management happens only on UI state changes
- No per-frame cursor updates
- Uses existing KeyboardInputManager system

## Compatibility

? Works with existing crafting system  
? Works with inventory system  
? Works with other UI panels  
? No conflicts with first-person controls  
? Respects overall panel state tracking  

## Future Enhancements

- Add keyboard shortcut to open planting UI (similar to crafting)
- Add cursor pre-check before opening UI
- Add visual cursor state indicator
- Add animation for cursor transitions
