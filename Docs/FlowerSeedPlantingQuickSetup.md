# Flower Seed Planting System - Quick Setup Guide

## 5-Minute Setup

### Step 1: Create Flower Seed Data Assets (2 min)

For each flower type:
```
Right-click ? Create > Farming > Flower Seed Data
```

**Example: Rose.asset**
- Seed Name: `Rose`
- Seed Icon: [Rose seed sprite]
- Flower Prefab: [Rose flower model prefab]

Repeat for other flowers (Daisy, Tulip, etc.)

### Step 2: Tag Your Soil Objects (1 min)

For each flower bed/soil GameObject:
1. Select in hierarchy
2. Set Tag to: `Soil`
3. Set Layer to: `Interactable`

### Step 3: Add SoilController Component (1 min)

1. Select soil GameObject
2. Add Component ? SoilController
3. Configure:
   - Soil Name: "Rose Bed" (or name)
   - Plant Spawn Point: (leave empty or set to transform above soil)
   - Highlight Material: (your highlight material)

### Step 4: Setup Controllers in Scene (1 min)

**FlowerSeedPlantingController:**
1. Create empty GameObject
2. Add Component ? FlowerSeedPlantingController
3. Set Placement Mask to "Interactable" layer
4. Set Valid/Invalid materials

**FlowerSeedPlantingUIManager:**
1. Create empty GameObject
2. Add Component ? FlowerSeedPlantingUIManager
3. Assign UIDocument with FlowerSeedPlanting.uxml
4. Add available flower seeds to the list

### Done! Test it:

1. Collect flower seeds from trees (final harvest)
2. Walk up to a soil bed
3. Press F to interact
4. Select a flower to plant
5. Click to place the flower

## Scene Setup Checklist

- [ ] FlowerSeedData assets created for each flower
- [ ] Soil GameObjects have "Soil" tag
- [ ] Soil GameObjects on "Interactable" layer
- [ ] SoilController added to soil GameObjects
- [ ] FlowerSeedPlantingController in scene
- [ ] FlowerSeedPlantingUIManager in scene
- [ ] UIDocument assigned with correct UXML
- [ ] Placement materials configured
- [ ] Flower prefabs set in FlowerSeedData

## Key Files

| File | Purpose |
|------|---------|
| `SoilController.cs` | Soil bed that holds flowers |
| `FlowerSeedPlantingController.cs` | Placement system (like crafting) |
| `FlowerSeedPlantingUIManager.cs` | Seed selection UI |
| `FlowerSeedPlanting.uxml` | UI layout |
| `FlowerSeedPlantingStyles.uss` | UI styling |

## Troubleshooting

**"Planting cancelled" - Can't plant flowers**
- Check layer mask includes "Interactable"
- Verify SoilController is on correct layer
- Ensure camera is assigned

**UI not showing**
- Check UIDocument is assigned
- Verify UXML path is correct
- Ensure FlowerSeedPlantingUIManager is in scene

**Raycast not detecting soil**
- Check soil layer is "Interactable"
- Verify collider is on soil GameObject
- Check placement mask in controller

**Inventory not updating**
- Verify InventoryManager.Instance exists
- Check FlowerSeeds are added as ItemType.FlowerSeeds
- Ensure OnInventoryChanged event fires

## System Flow

```
Interact(F) ? Show UI ? Select Flower ? Enter Placement Mode
     ?
Raycast updates preview ? Click to place ? Plant flower
     ?
Consume from inventory ? Spawn flower ? Return to normal
```

## Customization

### Change Placement Distance
In `FlowerSeedPlantingController`:
```csharp
public float placementMaxDistance = 50f; // Adjust this value
```

### Change Gizmo Size
```csharp
public float gizmoSize = 0.5f; // Adjust for visibility
```

### Disable Preview Materials
Set to null in inspector to use default materials.

## Next Steps

1. Add flower growth mechanics
2. Add harvesting mechanics
3. Add decorative variations
4. Add seasonal flowers
5. Add cross-breeding system
