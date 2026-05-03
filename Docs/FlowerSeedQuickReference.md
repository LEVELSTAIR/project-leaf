# Flower Seed Collection - Quick Reference Card

## 30-Second Overview

Seed trees now produce **flower seeds on their final harvest**. Each harvest yields fewer seeds with randomization:

```
Harvest 1: 10 seeds  (100% yield)
Harvest 2: 7 seeds   (65% yield)
Harvest 3: Flower seeds (any variant)
```

---

## Setup in 3 Steps

### Step 1: Open SeedData Asset
```
Assets/Data/Seeds/YourSeed.asset
```

### Step 2: Enable Flowers
```
Produces Flower Seeds:  TRUE
Max Harvests:           3
```

### Step 3: Add Flower Types
```
Flower Seed Yields:
  [+] Name: Rose Seed
      Min: 5, Max: 8
  [+] Name: White Rose Seed
      Min: 4, Max: 7
```

---

## Harvest Mechanics at a Glance

| Round | Formula | Example | Range |
|-------|---------|---------|-------|
| 1 | Base × 1.00 | 10 | 8-12 |
| 2 | Base × 0.65 | 6.5 | 5-8 |
| 3 | Base × 0.30 | 3 | 2-4 |
| 4+ | Flower Seeds | 5-10 | varies |

**Formula**: `(base × multiplier) ± 20%`

---

## Key Settings

```csharp
seedData.harvestYield = 10;              // Base amount
seedData.maxHarvests = 3;                // Rounds before depleted
seedData.producesFlowerSeeds = true;     // Enable flowers

// Add to flowerSeedYields list:
new FlowerSeedYield {
  flowerSeedName = "Rose Seed",
  minYield = 5,
  maxYield = 8
}
```

---

## Inventory Integration

```csharp
// Regular seeds (rounds 1-N)
InventoryManager.AddItem("Rose", ItemType.Seed, 10);

// Flower seeds (final harvest)
InventoryManager.AddItem("Rose Seed", ItemType.FlowerSeeds, 7);
```

---

## Tree Lifecycle

```
START ? HARVEST ROUND 1 ? HARVEST ROUND 2 ? HARVEST ROUND 3
                            (regular seeds)    (flower seeds)
                                                      ?
DEPLETED ? [Wait resetTime] ? RESET (harvestRound = 0)
```

---

## Common Configurations

### Multi-Variety Rose Bush
```
Max Harvests: 3
Base Yield: 12
Varieties: Rose Seed (8-12), White Rose Seed (6-10), Pink Rose Seed (5-8)
```

### Single Harvest Exotic
```
Max Harvests: 1
Base Yield: 1
Varieties: Hibiscus Seed (8-12), Red Hibiscus Seed (7-11)
```

### Low Yield Wildflower
```
Max Harvests: 4
Base Yield: 8
Varieties: Dandelion (4-6), Clover (3-5), Poppy (5-7)
```

---

## Debugging

**Check console for harvest details:**
```
[Cyan] Collecting seeds from 'Rose Bush' using SeedData: Rose x12
[Magenta] Adding seeds: Rose x10 (Round 1, Multiplier: 1.00)
[Green] Collected 10 Rose(s)! (Round 1/3)
```

**Common Issues:**
- Tree depletes after 1 harvest? ? Check `maxHarvests` value
- No flowers appearing? ? Check `producesFlowerSeeds` is TRUE
- Same flower always? ? Add more varieties to list

---

## Code Locations

**Modified Files:**
- `Assets/Scripts/Seeds/SeedData.cs` - New fields added
- `Assets/Scripts/Seeds/SeedTree.cs` - Harvest tracking implemented

**Documentation:**
- `Docs/FlowerSeedCollection.md` - Full system guide
- `Docs/FlowerSeedImplementationGuide.md` - Setup instructions
- `Docs/FlowerSeedExamples.md` - Configuration examples
- `Docs/FlowerSeedTechnicalReference.md` - Technical details

---

## Key Features

? **Diminishing Returns** - Each harvest yields less  
? **Randomization** - ±20% variance per harvest  
? **Multiple Varieties** - Different flower seeds per tree  
? **Harvest Tracking** - System knows which round you're on  
? **Backward Compatible** - Existing trees still work  
? **Automatic Reset** - Trees reset after cooldown period  

---

## One-Minute Test

1. Create SeedData with `producesFlowerSeeds = true`
2. Assign to tree in scene
3. Run game
4. Harvest tree 3 times
5. Observe: Seeds decrease each round, then flowers appear

**Expected Console Output:**
```
Round 1: ~10 seeds
Round 2: ~6-7 seeds
Round 3: ~5 flower seeds (random type)
```

---

## Need Help?

See these files:
- **Setup**: `FlowerSeedImplementationGuide.md`
- **Examples**: `FlowerSeedExamples.md`
- **Troubleshooting**: `FlowerSeedCollection.md`
- **Code Details**: `FlowerSeedTechnicalReference.md`
