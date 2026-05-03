# Flower Seed Collection - Implementation Guide

## Quick Start

### Step 1: Create FlowerSeedData Assets

For each flower variety, create a `FlowerSeedData` asset:

1. Right-click in your Assets folder
2. Select **Create > Farming > Flower Seed Data**
3. Name it (e.g., `Rose_Seed.asset`)
4. In the Inspector, set:
   - **Seed Name**: "Rose Seed" (displayed in inventory)
   - **Seed Icon**: Rose seed sprite
   - **Flower Prefab**: Rose flower GameObject prefab
5. Repeat for other flower varieties (White Rose Seed, Pink Rose Seed, etc.)

### Step 2: Update Your SeedData Asset

For any seed tree that should produce flower seeds:

1. Open the SeedData asset (e.g., `Assets/Data/Seeds/Acorn.asset`)
2. Expand the **Flower Seed Settings** section
3. Enable **Produces Flower Seeds** checkbox
4. Set **Max Harvests** to 3 (or desired number)
5. Add flower seed types:
   - Click "+" to add a new entry to **Flower Seed Yields**
   - **Flower Seed Data**: Drag the FlowerSeedData asset you created
   - **Min Yield**: 5 (minimum seeds to collect)
   - **Max Yield**: 8 (maximum seeds to collect)
   - Repeat for additional flower types

### Step 3: Configure SeedTree in Scene

1. Select the tree GameObject in your scene
2. In the Inspector, assign the updated SeedData asset
3. Verify **Max Harvests** matches your configuration (should be 3)
4. Ensure **Interaction Cooldown** and **Reset Time** are set appropriately

### Step 4: Test

1. Run the game
2. Interact with the tree (F key)
3. Observe:
   - First harvest: Regular seeds
   - Second harvest: Fewer regular seeds
   - Third harvest: Flower seeds appear in inventory

## Example Configurations

### Rose Patch Tree

**Create these FlowerSeedData assets:**
- `Rose_Seed.asset` (seedName: "Rose Seed")
- `White_Rose_Seed.asset` (seedName: "White Rose Seed")
- `Pink_Rose_Seed.asset` (seedName: "Pink Rose Seed")

**Then in SeedData for the Rose plant:**
```
Seed Name:          "Rose"
Harvest Yield:      12
Max Harvests:       3
Produces Flower Seeds: TRUE

Flower Seed Yields:
  [0] Flower Seed Data: Rose_Seed.asset
      Min: 8, Max: 12
  [1] Flower Seed Data: White_Rose_Seed.asset
      Min: 6, Max: 10
  [2] Flower Seed Data: Pink_Rose_Seed.asset
      Min: 5, Max: 8
```

**Harvest Sequence:**
- Round 1: ~12 Rose seeds
- Round 2: ~8 Rose seeds  
- Round 3: 5-12 random Rose variant seeds (with icon and prefab from FlowerSeedData)

### Wildflower Bush

**Create these FlowerSeedData assets:**
- `Dandelion_Seed.asset`
- `Clover_Seed.asset`
- `Poppy_Seed.asset`

```
Seed Name:          "Wildflower"
Harvest Yield:      8
Max Harvests:       4
Produces Flower Seeds: TRUE

Flower Seed Yields:
  [0] Flower Seed Data: Dandelion_Seed.asset
      Min: 4, Max: 6
  [1] Flower Seed Data: Clover_Seed.asset
      Min: 3, Max: 5
  [2] Flower Seed Data: Poppy_Seed.asset
      Min: 5, Max: 7
```

**Harvest Sequence:**
- Round 1: ~8 Wildflower seeds
- Round 2: ~5 Wildflower seeds
- Round 3: ~3 Wildflower seeds
- Round 4: 3-7 random wildflower variant seeds

### Sunflower Garden

**Create these FlowerSeedData assets:**
- `Sunflower_Seed.asset`
- `Dwarf_Sunflower_Seed.asset`

```
Seed Name:          "Sunflower"
Harvest Yield:      15
Max Harvests:       2
Produces Flower Seeds: TRUE

Flower Seed Yields:
  [0] Flower Seed Data: Sunflower_Seed.asset
      Min: 10, Max: 15
  [1] Flower Seed Data: Dwarf_Sunflower_Seed.asset
      Min: 8, Max: 12
```

**Harvest Sequence:**
- Round 1: ~15 Sunflower seeds
- Round 2: 8-15 random Sunflower variant seeds

## Harvest Mechanics Reference

### Diminishing Returns Multipliers

| Round | Multiplier | Example (Base: 10) | With Randomness |
|-------|------------|-------------------|-----------------|
| 1     | 1.00       | 10                | 8-12            |
| 2     | 0.65       | 6.5 ? 7           | 5-9             |
| 3     | 0.30       | 3.0 ? 3           | 2-4             |
| 4     | 0.30       | 3.0 ? 3           | 2-4             |

Each harvest applies ±20% randomization.

## Inventory Integration

The system automatically handles inventory management:

```csharp
// Regular seeds (from SeedData)
InventoryManager.AddItem("Rose", ItemType.Seed, 10);

// Flower seeds (from FlowerSeedData)
InventoryManager.AddItem("Rose Seed", ItemType.FlowerSeeds, 7);
```

Both types can be used for:
- Planting and farming
- Crafting recipes
- Trading
- Quest objectives

## Debugging

### Console Messages

Watch the console for harvest details:

```
[Cyan] Collecting seeds from 'Rose Bush' using SeedData: Rose x12
[Magenta] Adding seeds: Rose x10 (Round 1, Multiplier: 1.00)
[Green] Collected 10 Rose(s)! (Round 1/3)
```

### Common Issues

**Issue: Tree depletes after first harvest**
- Check `maxHarvests` value (should be > 1)
- Verify SeedData is assigned correctly

**Issue: No flower seeds appearing**
- Ensure `producesFlowerSeeds` is enabled on SeedData
- Verify `flowerSeedYields` list is not empty
- Check that each entry has a **valid FlowerSeedData reference** (not null)
- Check that `maxHarvests` > 1

**Issue: Same flower seed every time**
- System randomly selects from `flowerSeedYields`
- With only 1 entry, you'll always get the same type
- Add more flower seed varieties to increase randomness

**Issue: Console warning about null FlowerSeedData**
- Check that you assigned a FlowerSeedData asset to each entry
- Verify the asset exists and is not deleted

## Advanced: Using FlowerSeedData in Code

You can now access flower properties directly:

```csharp
// In custom code that uses flower seeds
FlowerSeedYield selectedFlower = seedData.flowerSeedYields[0];

// Access FlowerSeedData properties
string name = selectedFlower.flowerSeedData.seedName;
Sprite icon = selectedFlower.flowerSeedData.seedIcon;
GameObject prefab = selectedFlower.flowerSeedData.flowerPrefab;
```

This is useful for:
- Farming systems that need flower prefabs
- UI systems that need flower icons
- Crafting systems that use specific flower types

## Integration Checklist

- [ ] FlowerSeedData assets created for each flower variety
- [ ] Each FlowerSeedData has seedName, icon, and prefab set
- [ ] SeedData assets updated with flower seed types
- [ ] Each flowerSeedYields entry references a FlowerSeedData asset
- [ ] SeedTree components have correct `maxHarvests` values
- [ ] `producesFlowerSeeds` enabled where needed
- [ ] At least one `flowerSeedYields` entry per flower tree
- [ ] HUDManager and InventoryManager properly referenced
- [ ] Tested first harvest (regular seeds)
- [ ] Tested subsequent harvests (diminishing amounts)
- [ ] Tested final harvest (flower seeds appear with correct data)
- [ ] Tested tree reset and re-harvest cycle
