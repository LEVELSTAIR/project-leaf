# Flower Seed Planting System

## Overview

The Flower Seed Planting System allows players to plant flower seeds collected from trees into flower beds with soil. The system uses a positioning-based approach similar to the crafting system, where players see a preview before planting.

## Architecture

### Components

#### 1. **SoilController.cs**
The soil/flower bed that can have flowers planted in it. Similar to a plant pot but for the ground.

**Key Features:**
- Interactable object with tag "Soil" and layer "Interactable"
- Shows plant/harvest prompts
- Can have one flower planted at a time
- Handles planting and harvesting
- Visual feedback with highlight material

**Key Methods:**
- `PlantFlowerSeed(FlowerSeedData, int)` - Plants a flower seed
- `HarvestFlower()` - Harvests the planted flower
- `HasFlower()` - Check if soil has a flower
- `GetPlantedFlower()` - Get the flower data

#### 2. **FlowerSeedPlantingController.cs**
Main controller for the planting placement system. Mirrors the crafting system's placement mechanic.

**Key Features:**
- Handles placement preview and positioning
- Raycast-based placement validation
- Visual feedback (valid/invalid material)
- Debug gizmos for visualization
- Input handling (Left Click to plant, X/RMB to cancel)

**Key Methods:**
- `StartPlantingMode(FlowerSeedData, SoilController, int)` - Begin planting
- `HandlePlacement()` - Update preview position and handle input
- `ConfirmPlanting(Vector3)` - Finalize the planting
- `CancelPlanting()` - Cancel and cleanup
- `IsCurrentlyPlacing()` - Check if actively placing

#### 3. **FlowerSeedPlantingUIManager.cs**
UI manager for selecting which flower seed to plant. Displays available flower seeds from inventory.

**Key Features:**
- Shows available flower seeds with icons and quantities
- Tracks inventory amounts in real-time
- Disables buttons when out of stock
- Integrates with InventoryManager
- Shows planting UI when interacting with soil

**Key Methods:**
- `ShowUI(SoilController)` - Display UI for a specific soil
- `RefreshUI()` - Update amounts and button states
- `SetUIVisible(bool)` - Toggle UI visibility

## Setup Instructions

### Step 1: Create a FlowerSeedData Asset

For each flower type you want to plant:

1. Right-click in Assets folder
2. Create > Farming > Flower Seed Data
3. Configure:
   - **Seed Name**: "Rose" (or your flower name)
   - **Seed Icon**: Flower seed icon sprite
   - **Flower Prefab**: The flower 3D model/prefab

### Step 2: Add SoilController to Soil Beds

1. Select your soil/flower bed GameObject in the scene
2. Add Component > SoilController
3. Configure:
   - **Soil Name**: Name for this bed (e.g., "Rose Bed")
   - **Plant Spawn Point**: Where the flower should appear (optional, defaults to soil position)
   - **Highlight Material**: Material to show when hovering
   - **Plant Effect**: Particle system for planting (optional)
   - **Plant Sound**: Audio clip when planting (optional)

4. Add tag "Soil" to the GameObject
5. Set layer to "Interactable"

### Step 3: Configure FlowerSeedPlantingController

1. Create an empty GameObject in your scene
2. Add Component > FlowerSeedPlantingController
3. Configure:
   - **Placement Mask**: Set to layer containing Soil objects (usually "Interactable")
   - **Valid/Invalid Placement Material**: Materials for preview feedback
   - **Placement Camera**: Reference to main camera (auto-detects if empty)
   - **Debug Settings**: Toggle gizmos and adjust gizmo size

### Step 4: Configure FlowerSeedPlantingUIManager

1. Create an empty GameObject in your scene
2. Add Component > FlowerSeedPlantingUIManager
3. Assign:
   - **UIDocument**: Reference to your UI Toolkit document
   - **Available Flower Seeds**: List of FlowerSeedData assets players can plant

4. Create a UIDocument and reference `FlowerSeedPlanting.uxml`

### Step 5: Test

1. Collect flower seeds from trees (final harvest)
2. Interact with a soil object (F key)
3. Select a flower seed to plant
4. See placement preview
5. Click to plant the flower at the location

## Workflow

```
Player collects Flower Seeds from tree (via SeedTree.cs)
        ?
Player interacts with Soil bed
        ?
FlowerSeedPlantingUIManager shows available flower seeds
        ?
Player selects a flower seed
        ?
FlowerSeedPlantingController enters planting mode
        ?
Placement preview appears following camera
        ?
Player aims and clicks to confirm placement
        ?
SoilController.PlantFlowerSeed() called
        ?
Flower seeds consumed from inventory
        ?
Flower GameObject spawned at soil
        ?
Flower is now growing/harvested
```

## Inventory Integration

**Flower Seeds** are stored as `ItemType.FlowerSeeds` in the inventory:
- Added when collecting from trees (final harvest)
- Removed when planting in soil
- Can be tracked and displayed in UI

## Placement Preview System

Similar to crafting, the planting system uses:

1. **Valid Placement** (Green Material):
   - Raycast hits the target soil
   - Soil doesn't already have a flower

2. **Invalid Placement** (Red Material):
   - Raycast misses or hits something else
   - Target soil already has a flower

3. **Controls**:
   - **Left Click**: Confirm planting at preview location
   - **X Key**: Cancel planting
   - **Right Mouse Button**: Cancel planting

## Debug Features

- **Placement Gizmos**: Visualizes raycast hits and preview positions
  - Blue sphere: Valid hit on soil
  - Orange sphere: Invalid hit or no hit
  - Wireframe cube: Placement bounds
  - Line: Ray from camera to hit point

- **Console Output**:
  - Planting start/confirmation
  - Flower seed consumption
  - Success/error messages

## Files Modified/Created

### New Files:
- `Assets\Scripts\Garden\SoilController.cs` - Soil bed controller
- `Assets\Scripts\Garden\FlowerSeedPlantingController.cs` - Planting system controller
- `Assets\Scripts\UI\FlowerSeedPlantingUIManager.cs` - UI manager
- `Assets\UI Toolkit\Planting\FlowerSeedPlanting.uxml` - UI layout
- `Assets\UI Toolkit\Planting\FlowerSeedPlantingStyles.uss` - UI styles

### Dependencies:
- `FlowerSeedData.cs` - Defines flower types
- `InventoryManager.cs` - Inventory tracking
- `HUDManager.cs` - Messages and feedback
- `IInteractable.cs` - Interface for interactable objects

## Example Setup

### Rose Bed Example

1. Create FlowerSeedData for "Rose":
   - Seed Name: "Rose"
   - Flower Prefab: Rose flower model
   - Icon: Rose seed sprite

2. Add SoilController to soil GameObject:
   - Soil Name: "Rose Bed"
   - Plant Spawn Point: Top of soil bed

3. Tag & Layer:
   - Tag: "Soil"
   - Layer: "Interactable"

4. Player workflow:
   - Harvest trees ? Get flower seeds
   - Interact with Rose Bed (F)
   - Select "Rose" from UI
   - Click to place
   - Rose flower grows in the bed

## Performance Considerations

- **Raycast**: Filtered by layer mask for efficiency
- **Preview**: Single preview object reused across all plantings
- **UI Updates**: Only refresh on inventory changes
- **Collider Removal**: Prevents blocking raycast before planting

## Future Enhancements

- Multiple flowers per soil bed
- Cross-breeding mechanics
- Flower growth stages
- Seasonal flowers
- Flower harvesting for resources
- Decorative flower arrangements
