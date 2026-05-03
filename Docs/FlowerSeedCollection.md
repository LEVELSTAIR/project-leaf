# Flower Seed Collection System

## Overview

The Flower Seed Collection system allows seed trees to produce flower seeds after reaching their maximum regular harvest count. This system implements:

1. **Diminishing Returns** - Each harvest round yields fewer seeds with randomization
2. **Multiple Flower Seed Types** - Trees can produce different varieties of flower seeds
3. **Harvest Tracking** - Trees track which harvest round they're on (1st, 2nd, 3rd, etc.)

## How It Works

### Harvest Rounds with Diminishing Returns

When a tree is harvested multiple times before resetting, each harvest produces fewer seeds:

- **Round 1**: 100% of base yield (e.g., 10 seeds)
- **Round 2**: 80% of base yield (e.g., 8 seeds)  
- **Round 3**: 50% of base yield (e.g., 5 seeds)

Each amount is randomized by ±20% to create variation.

Example:
```
Round 1: 10 seeds (randomized to 8-12)
Round 2: 8 seeds (randomized to 6-10)
Round 3: 5 seeds (randomized to 4-6)
```

### Flower Seed Production

When a tree reaches its maximum harvest count (`maxHarvests`), subsequent harvests produce **flower seeds** instead of regular seeds:

- Only flower seeds are collected in the final harvest round
- A random flower seed type is selected from the tree's flower seed yields
- The amount is randomized between `minYield` and `maxYield` for each flower type

Example:
```
If maxHarvests = 3 and flowerSeedYields contains:
  - Rose Seeds (5-8 per harvest)
  - Daisy Seeds (4-6 per harvest)
  - Tulip Seeds (6-9 per harvest)

Round 1: 10 regular seeds
Round 2: 8 regular seeds
Round 3: Random flower seed (e.g., 7 Rose Seeds)
```

## Configuration

### In SeedData ScriptableObject

Add flower seed configuration to your seed asset:

```csharp
[Header("Flower Seed Settings")]
public bool producesFlowerSeeds = false;           // Enable flower seed production
public List<FlowerSeedYield> flowerSeedYields = new List<FlowerSeedYield>();
```

Each `FlowerSeedYield` entry contains:
- **flowerSeedName** - The name of the flower seed (e.g., "Rose Seed")
- **minYield** - Minimum seeds to collect (e.g., 5)
- **maxYield** - Maximum seeds to collect (e.g., 8)

### In SeedTree Inspector

1. Set `maxHarvests` to the desired number (e.g., 3)
2. Enable `producesFlowerSeeds` on the SeedData asset
3. Add entries to `flowerSeedYields` for each flower type the tree can produce

## Implementation Details

### Diminishing Returns Formula

```
adjustedAmount = baseAmount * multiplier
```

Where `multiplier` for each round is:
- Round 0: 1.0 (100%)
- Round 1: 0.65 (65%)
- Round 2: 0.3 (30%)
- Round n: max(0.3, 1.0 - (n * 0.35))

### Randomization

Each harvest applies ±20% randomness:

```csharp
int randomVariance = adjustedAmount * 0.2;
finalAmount = Random.Range(adjustedAmount - randomVariance, 
                           adjustedAmount + randomVariance + 1);
```

### Flower Seed Selection

When collecting flower seeds, the system:
1. Randomly selects one flower seed type from `flowerSeedYields`
2. Generates a random amount between that type's min and max
3. Adds it to the player's inventory as `ItemType.FlowerSeeds`

## Reset Behavior

When a tree resets (after `resetTime` seconds):
- `harvestRound` is reset to 0
- Tree can be harvested again from round 1
- All harvest counts begin fresh

## Debug Output

The system provides detailed console logging:

```
[Round 1] Collecting seeds from 'Oak Tree' using SeedData: Acorn x10
[Round 1] Adding seeds: Acorn x10 (Round 1, Multiplier: 1.00)
[Round 1] Collected 10 Acorn(s)! (Round 1/3)

[Round 2] Adding seeds: Acorn x7 (Round 2, Multiplier: 0.65)
[Round 2] Collected 7 Acorn(s)! (Round 2/3)

[Round 3] Collecting flower seeds from 'Oak Tree': Rose Seed x6
[Round 3] Collected 6 Rose Seed(s)!
```

## Common Scenarios

### Scenario 1: Basic Flower Seed Tree

**Configuration:**
- `maxHarvests`: 3
- `seedName`: "Acorn"
- `harvestYield`: 10
- `producesFlowerSeeds`: true
- `flowerSeedYields`: [Rose Seeds (5-8), Daisy Seeds (4-6)]

**Result:**
1. First harvest: ~10 Acorns
2. Second harvest: ~6-7 Acorns
3. Third harvest: 5-8 random flower seeds

### Scenario 2: Single Harvest Tree

**Configuration:**
- `maxHarvests`: 1
- `producesFlowerSeeds`: true
- `flowerSeedYields`: [Rose Seeds (8-12)]

**Result:**
1. First harvest: Only flower seeds (8-12 Rose Seeds)

### Scenario 3: Regular Tree (No Flower Seeds)

**Configuration:**
- `maxHarvests`: 3
- `producesFlowerSeeds`: false
- `flowerSeedYields`: (empty)

**Result:**
1. First harvest: ~10 seeds
2. Second harvest: ~6-7 seeds
3. Third harvest: ~3-4 seeds (tree depletes)

## Code Integration

### SeedTree Properties

```csharp
private int harvestRound = 0;              // Current harvest round (0-indexed)
private float[] harvestReductionMultipliers; // Precalculated multipliers
```

### Key Methods

- `CollectSeeds()` - Collects regular seeds with diminishing returns
- `CollectFlowerSeeds()` - Collects flower seeds (final harvest round)
- `ResetTree()` - Resets harvest counter and state

## Inventory Integration

- Regular seeds are added as `ItemType.Seed`
- Flower seeds are added as `ItemType.FlowerSeeds`
- Both integrate with the existing InventoryManager system

## Future Enhancements

Potential improvements:
- Procedural flower seed distribution (weighted probabilities)
- Seasonal flower seed variations
- Quality tiers for flower seeds
- Achievement tracking for rare flower seeds
- Visual tree changes reflecting harvest progress
