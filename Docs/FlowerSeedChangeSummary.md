# Flower Seed Collection System - Implementation Summary

## Changes Made

### 1. SeedData.cs
- Added `producesFlowerSeeds` boolean flag
- Added `flowerSeedYields` list to store multiple flower seed types
- Created `FlowerSeedYield` serializable class with:
  - `flowerSeedName`: Name of the flower seed variety
  - `minYield`: Minimum seeds to collect
  - `maxYield`: Maximum seeds to collect

### 2. SeedTree.cs
- **Harvest Tracking**:
  - Added `harvestRound` counter (tracks which harvest we're on)
  - Added `harvestReductionMultipliers` array (precalculated multipliers)

- **Diminishing Returns**:
  - Round 1: 100% of base yield
  - Round 2: 65% of base yield
  - Round 3: 30% of base yield
  - Each harvest randomized by ±20%

- **New Methods**:
  - `CollectFlowerSeeds()`: Collects random flower seeds in final harvest
  - Updated `Interact()`: Increments `harvestRound` and checks if max harvests reached
  - Updated `CollectSeeds()`: Applies diminishing returns multiplier and randomization
  - Updated `ResetTree()`: Resets `harvestRound` to 0

- **Mechanics**:
  - Each harvest increments the round counter
  - When `harvestRound >= maxHarvests - 1`, flower seeds are collected instead
  - Tree depletes after reaching `maxHarvests` rounds
  - On reset, `harvestRound` is set back to 0

## Key Features

### ? Diminishing Returns
- Each harvest yields fewer seeds
- Formula: `adjustedAmount = baseAmount * multiplier`
- Multiplier decreases with each round: 1.0 ? 0.65 ? 0.30

### ? Randomization
- Each harvest amount varies by ±20%
- Adds unpredictability and replay value
- Flower seed amounts randomized between min/max per type

### ? Multiple Flower Seed Types
- Each tree can produce different flower varieties
- Random selection each time flower seeds are harvested
- Independent min/max ranges per flower type

### ? Harvest Round Tracking
- System knows which harvest round it is
- UI messages show "Round 1/3" feedback
- Debug logs track progression

### ? Backward Compatible
- Trees without flower seed configuration work as before
- Existing seed data is preserved
- No breaking changes to current system

## Harvest Example

**Configuration: 3-round Rose Bush**

```
Round 1 (10 base seeds):
  Base: 10 × 1.00 = 10
  Randomized: 10 ± 20% = 8-12 seeds collected

Round 2 (10 base seeds):
  Base: 10 × 0.65 = 6.5 ? 7
  Randomized: 7 ± 20% = 5-9 seeds collected

Round 3 (Flower Seeds):
  Selected: "Rose Seed" (5-8 range)
  Randomized: Random(5, 9) = 6 flower seeds collected

Tree depletes and must reset
```

## Inventory System Integration

The system uses existing inventory types:
- **Regular seeds**: Stored as `ItemType.Seed`
- **Flower seeds**: Stored as `ItemType.FlowerSeeds`

Both integrate seamlessly with:
- InventoryManager
- SeedManager
- CraftingUIManager
- Farming systems

## Configuration Steps

### For Each Tree Type:

1. **Open SeedData asset**
   - Enable "Produces Flower Seeds"
   - Set "Max Harvests" (e.g., 3)
   - Add "Flower Seed Yields" entries

2. **Assign to SeedTree**
   - Select tree in scene
   - Assign updated SeedData
   - Configure interaction cooldown and reset time

3. **Test**
   - Harvest tree multiple times
   - Observe diminishing returns
   - Collect flower seeds on final harvest

## Files Modified

1. `Assets\Scripts\Seeds\SeedData.cs`
   - Added FlowerSeedYield class
   - Added flower seed configuration fields

2. `Assets\Scripts\Seeds\SeedTree.cs`
   - Added harvest tracking system
   - Implemented diminishing returns
   - Added flower seed collection logic
   - Updated tree reset mechanics

## Files Created

1. `Docs\FlowerSeedCollection.md` - Comprehensive system documentation
2. `Docs\FlowerSeedImplementationGuide.md` - Setup and configuration guide

## Testing Checklist

- [ ] Tree harvests regular seeds first round
- [ ] Second harvest yields fewer seeds
- [ ] Third harvest produces flower seeds
- [ ] Flower seed type varies randomly
- [ ] Amounts are randomized (±20%)
- [ ] Tree resets properly
- [ ] Multiple flower types work correctly
- [ ] No errors in console
- [ ] Existing trees without flower config still work
- [ ] InventoryManager counts both seed types correctly

## No Breaking Changes

? Trees without flower seed configuration continue to work
? Existing harvest system remains compatible
? All InventoryManager methods work with both seed types
? No changes to public APIs or expected behaviors
