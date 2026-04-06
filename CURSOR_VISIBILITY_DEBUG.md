# PlantingUI Cursor Visibility - Debugging Guide

## Issue
Cursor is not showing when PlantingUI is displayed, even though `Cursor.visible = true` is set.

## Root Cause Analysis

There are multiple systems managing cursor state:

### 1. **GameManager** - Controls cursor based on game state
- **File:** `Assets\Scripts\Managers\GameManager.cs`
- Sets cursor lock/visibility based on `GameState` enum
- States: Playing, Login, Inventory, Paused

### 2. **KeyboardInputManager** - Manages UI panel states
- **File:** `Assets\Scripts\KeyboardInputManager.cs`
- Property: `IsAnyPanelOpen` - checks if any UI panel is currently visible
- Controls cursor based on panel states (Inventory, Book, Escape Menu, etc.)

### 3. **PlantingUIManager** - Manages seed selection UI
- **File:** `Assets\Scripts\UI\PlantingUIManager.cs`
- Opens when player interacts with PlantPot
- Sets `Time.timeScale = 0f` (pauses game)
- Must coordinate cursor state with other managers

## Solution Implementation

### Changes Made to PlantingUIManager

#### 1. ShowPlantingUI() - Explicit Cursor Unlock
```csharp
// Handle cursor and game state
UnityEngine.Cursor.lockState = CursorLockMode.None;
UnityEngine.Cursor.visible = true;
```
- Set BEFORE Time.timeScale change
- Uses `UnityEngine.Cursor` explicitly to avoid UIElements ambiguity

#### 2. HidePlantingUI() - Intelligent Cursor Reset
```csharp
public void HidePlantingUI()
{
    plantingPanel.style.display = DisplayStyle.None;
    isUIVisible = false;
    Time.timeScale = 1f;
    ResetCursor(); // Checks other panels before locking
}
```

#### 3. ResetCursor() - Panel-Aware Locking
```csharp
private void ResetCursor()
{
    if (KeyboardInputManager.Instance != null)
    {
        if (!KeyboardInputManager.Instance.IsAnyPanelOpen)
        {
            // No other panels open, lock the cursor
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        // else: Another panel is open, keep cursor visible
    }
}
```

## Troubleshooting Steps

### Step 1: Check Debug Logs
When the Planting UI opens, you should see in the Console:
```
[PlantingUI] Showing UI - Cursor visible: True, LockState: None
```

When it closes:
```
[PlantingUI] ResetCursor - IsAnyPanelOpen: False
[PlantingUI] Cursor locked (no other panels open)
```

### Step 2: Verify Cursor Settings in Scene
1. In Unity Editor, go to **Edit ? Project Settings ? Player**
2. Find **Resolution and Presentation**
3. Check "Cursor Visible" in inspector (should be toggled by code, not forced)

### Step 3: Check for Conflicting Cursor Locks

Search for other places modifying cursor state:
```
Assets\Scripts\Managers\GameManager.cs - Sets cursor based on GameState
Assets\Scripts\KeyboardInputManager.cs - Sets cursor based on panel state
Assets\Scripts\UI\PlantingUIManager.cs - Sets cursor when UI opens
```

### Step 4: Ensure GameManager Compatibility

The PlantingUI pauses the game but does NOT change GameState.
If GameState affects cursor, you may need to:

**Option A:** Update GameManager when PlantingUI opens
```csharp
if (GameManager.Instance != null)
{
    GameManager.Instance.SetState(GameState.Paused);
}
```

**Option B:** Make GameManager aware of PlantingUI
Add to GameManager's state check:
```csharp
bool cursorShouldBeVisible = GameManager.IsUIVisible 
    || PlantingUIManager.Instance?.IsUIVisible 
    || KeyboardInputManager.Instance?.IsAnyPanelOpen;
```

## Testing Checklist

- [ ] PlantingUI appears when interacting with pot
- [ ] Cursor becomes visible immediately
- [ ] Cursor can move over seed buttons
- [ ] Buttons are clickable with mouse
- [ ] Escape key closes UI and hides cursor
- [ ] Cursor remains visible if Inventory is also open
- [ ] Cursor locks when PlantingUI closes (if no other panels open)

## Key Code Locations

| File | Method | Purpose |
|------|--------|---------|
| PlantingUIManager.cs | ShowPlantingUI() | Unlocks cursor |
| PlantingUIManager.cs | HidePlantingUI() | Hides UI |
| PlantingUIManager.cs | ResetCursor() | Smart cursor locking |
| KeyboardInputManager.cs | IsAnyPanelOpen | Panel state tracker |
| GameManager.cs | SetState() | Game state manager |
