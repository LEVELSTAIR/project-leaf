# Flower Seed Collection - Technical Reference

## Architecture Overview

```
SeedTree (MonoBehaviour)
??? Harvest Tracking
?   ??? harvestRound (current round: 0, 1, 2, ...)
?   ??? harvestReductionMultipliers (float[])
??? Seed Collection
?   ??? CollectSeeds() - Regular seeds with diminishing returns
?   ??? CollectFlowerSeeds() - Flower seeds on final harvest
??? Tree State Management
    ??? isDepleted
    ??? canInteract
    ??? ResetTree()

SeedData (ScriptableObject)
??? Base Fields (unchanged)
?   ??? seedName
?   ??? harvestYield
?   ??? maxHarvests
??? New Fields
    ??? producesFlowerSeeds (bool)
    ??? flowerSeedYields (List<FlowerSeedYield>)

FlowerSeedYield (Serializable)
??? flowerSeedName (string)
??? minYield (int)
??? maxYield (int)
```

## State Transitions

```
READY
  ? (Player interacts)
COLLECTING (Round 1, 2, ...)
  ?? Regular seeds ? READY (cooldown)
  ?? Final harvest ? FlowerSeeds
  ?
DEPLETED
  ? (After resetTime)
READY (harvestRound = 0)
```

## Pseudocode Flow

### Harvest Interaction
```
OnInteract():
  if isDepleted:
    CheckReset()
    return

  if !canInteract:
    return

  if harvestRound < maxHarvests - 1:
    CollectSeeds()         // Regular seeds
  else:
    CollectFlowerSeeds()   // Flower seeds

  harvestRound++
  canInteract = false

  if harvestRound >= maxHarvests:
    isDepleted = true
```

### Diminishing Returns Calculation
```
CollectSeeds():
  baseAmount = seedData.harvestYield
  multiplier = harvestReductionMultipliers[harvestRound]

  adjustedAmount = baseAmount * multiplier
  adjustedAmount = Round(adjustedAmount)

  randomVariance = adjustedAmount * 0.2
  finalAmount = Random(adjustedAmount - variance, 
                       adjustedAmount + variance)
  finalAmount = Max(1, finalAmount)

  AddToInventory(nameToAdd, finalAmount)
```

### Flower Seed Collection
```
CollectFlowerSeeds():
  selectedFlower = Random from flowerSeedYields

  minAmount = selectedFlower.minYield
  maxAmount = selectedFlower.maxYield
  finalAmount = Random(minAmount, maxAmount + 1)

  AddToInventory(selectedFlower.flowerSeedName, 
                 ItemType.FlowerSeeds, 
                 finalAmount)
```

## Key Formulas

### Multiplier Initialization
```
For each round i from 0 to maxHarvests-1:
  multiplier[i] = Max(0.3, 1.0 - (i * 0.35))

Resulting multipliers:
  i=0: 1.0 - 0.00 = 1.00
  i=1: 1.0 - 0.35 = 0.65
  i=2: 1.0 - 0.70 = 0.30
  i=3: 1.0 - 1.05 = 0.30 (clamped)
```

### Amount with Randomization
```
variance = amount * 0.20
finalAmount = Random.Range(amount - variance, 
                          amount + variance + 1)

Example: amount = 10
  variance = 2
  range = [8, 12]
```

## Event Flow

```
1. Player presses F near tree
   ?
2. Interact() called
   ?? Check depleted state
   ?? Check cooldown
   ?? Call CollectSeeds() or CollectFlowerSeeds()
   ?  ?? Calculate amount with diminishing returns
   ?  ?? Add to inventory
   ?  ?? Show HUD message
   ?? Increment harvestRound
   ?? Check if max harvests reached
   ?? Play effects (particle, sound)
   ?? Start animations (shake, flash)
   ?? Set cooldown timers
   ?
3. Cooldown timer running (interactionCooldown)
   ?
4. After cooldown expires:
   ?? If harvestRound < maxHarvests:
   ?  ?? Ready for next harvest
   ?? Else:
      ?? Mark as depleted
      ?? Start reset timer (resetTime)
   ?
5. After reset timer expires:
   ?? Call ResetTree()
   ?? Reset harvestRound = 0
   ?? Set isDepleted = false
   ?? Ready for harvest cycle again
```

## Inventory Integration

### Storage Format
```
// Regular seeds (collected rounds 1-N)
InventoryManager.AddItem(
  itemName: seedData.seedName,           // "Rose"
  itemType: ItemType.Seed,
  amount: calculatedAmount               // 10
)

// Flower seeds (collected on final harvest)
InventoryManager.AddItem(
  itemName: flowerSeedName,              // "Rose Seed"
  itemType: ItemType.FlowerSeeds,
  amount: randomAmount                   // 7
)
```

### Retrieval
```
// Get regular seeds
int count = InventoryManager.GetItemAmount("Rose", ItemType.Seed);

// Get flower seeds
int count = InventoryManager.GetItemAmount("Rose Seed", ItemType.FlowerSeeds);

// Check if player has enough
bool has = InventoryManager.HasItem("Rose Seed", ItemType.FlowerSeeds, 5);
```

## Performance Considerations

### Memory
- **harvestReductionMultipliers**: O(maxHarvests) ? typically 3-5 floats
- **flowerSeedYields**: O(flowerVarieties) ? typically 1-4 entries
- Per-tree overhead: ~50 bytes

### CPU (per harvest)
- Multiplier lookup: O(1)
- Randomization: O(1)
- Inventory add: O(n) where n = inventory slots (depends on InventoryManager)
- Random selection: O(flowerVarieties) typically O(1-4)

## Debug Methods

### Console Output Pattern
```
[Color] Message explaining action
Color codes:
  Cyan - Debug info (SeedData being used)
  Magenta - Amount calculations
  Green - Success messages
  Yellow - Missing data warnings
  Red - Errors
```

### Useful Debug Checks
```
// Add to Start() to verify configuration:
if (seedData != null && seedData.producesFlowerSeeds)
{
  Debug.Log($"Tree configured for {seedData.maxHarvests} harvests");
  Debug.Log($"Flower varieties: {seedData.flowerSeedYields.Count}");
}

// Add to CollectSeeds() to verify amounts:
Debug.Log($"Round {harvestRound}: {baseAmount} × {multiplier:F2} = {adjustedAmount}");
```

## Extension Points

### To Change Multiplier Formula
Edit in `Start()`:
```csharp
// Current (soft decline)
harvestReductionMultipliers[i] = Mathf.Max(0.3f, 1.0f - (i * 0.35f));

// Alternative 1 (steep decline)
harvestReductionMultipliers[i] = Mathf.Max(0.2f, 1.0f - (i * 0.5f));

// Alternative 2 (gentle decline)
harvestReductionMultipliers[i] = Mathf.Max(0.4f, 1.0f - (i * 0.2f));

// Alternative 3 (exponential)
harvestReductionMultipliers[i] = Mathf.Pow(0.7f, i);
```

### To Change Randomization Percentage
Edit in `CollectSeeds()`:
```csharp
// Current (±20%)
int randomVariance = Mathf.RoundToInt(adjustedAmount * 0.2f);

// Alternative 1 (±10% - more consistent)
int randomVariance = Mathf.RoundToInt(adjustedAmount * 0.1f);

// Alternative 2 (±30% - more chaotic)
int randomVariance = Mathf.RoundToInt(adjustedAmount * 0.3f);
```

### To Add Weighted Flower Selection
Replace in `CollectFlowerSeeds()`:
```csharp
// Simple version (current)
FlowerSeedYield selected = seedData.flowerSeedYields[
  Random.Range(0, seedData.flowerSeedYields.Count)
];

// Weighted version (requires weight property)
float totalWeight = seedData.flowerSeedYields.Sum(f => f.weight);
float rnd = Random.value * totalWeight;
float sum = 0;
foreach (var flower in seedData.flowerSeedYields)
{
  sum += flower.weight;
  if (rnd <= sum)
  {
    selected = flower;
    break;
  }
}
```

## Testing Checklist

```
[] Round 1: Base amount collected (100%)
[] Round 2: Reduced amount collected (~65%)
[] Round 3: Further reduced amount collected (~30%)
[] Randomization: ±20% variance observed
[] Flower seeds: Only on final harvest
[] Variety: Different flower types collected randomly
[] Reset: Tree resets after resetTime
[] State: isDepleted flag works correctly
[] Cooldown: interactionCooldown enforced
[] UI Messages: Correct feedback shown
[] Inventory: Both Seed and FlowerSeeds types stored
[] Console: Debug logs show expected values
[] Edge cases: maxHarvests=1 works (flower only)
[] Edge cases: No flower config works (regular only)
```

## Known Limitations

1. **Linear randomness** - Uses simple ±20% around calculated value
   - Could be enhanced with Poisson or Gaussian distributions

2. **Fixed diminishing formula** - Current implementation uses (1.0 - i*0.35)
   - Could be parameterized in SeedData for per-tree customization

3. **Single flower per harvest** - Final harvest selects only one flower type
   - Could be extended to collect multiple types in same harvest

4. **No weighted probability** - All flower types equally likely
   - Could add weight field to FlowerSeedYield for rarity tiers

## Future Enhancements

- [ ] Seasonal flower seed variations
- [ ] Quality tiers for flower seeds
- [ ] Procedural flower seed generation
- [ ] Achievement tracking for rare flowers
- [ ] Tree visual changes reflecting harvest stage
- [ ] NPC trading for rare flower seeds
- [ ] Crossbreeding mechanics between seed types
