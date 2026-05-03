# Flower Seed Configuration Examples

This document provides copy-paste ready configurations for common flower seed tree types.

## Example 1: Rose Garden Tree

**Asset Name**: `Rose.asset`

**SeedData Settings**:
```
Seed Name:              Rose
Seed Icon:              [Rose sprite]
Growth Time:            60
Water Required:         30
Harvest Yield:          12
Max Harvests:           3
Oxygen Area Radius:     10

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Rose Seed
      Min Yield: 8
      Max Yield: 12
  [1] Flower Seed Name: White Rose Seed
      Min Yield: 6
      Max Yield: 10
  [2] Flower Seed Name: Pink Rose Seed
      Min Yield: 5
      Max Yield: 8
```

**Expected Harvest Sequence**:
- Round 1: 10-14 Rose seeds
- Round 2: 7-10 Rose seeds
- Round 3: 5-12 random rose variety seeds

---

## Example 2: Wildflower Meadow

**Asset Name**: `Wildflower.asset`

**SeedData Settings**:
```
Seed Name:              Wildflower
Seed Icon:              [Wildflower sprite]
Growth Time:            45
Water Required:         25
Harvest Yield:          8
Max Harvests:           4
Oxygen Area Radius:     8

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Dandelion Seed
      Min Yield: 4
      Max Yield: 6
  [1] Flower Seed Name: Clover Seed
      Min Yield: 3
      Max Yield: 5
  [2] Flower Seed Name: Poppy Seed
      Min Yield: 5
      Max Yield: 7
  [3] Flower Seed Name: Buttercup Seed
      Min Yield: 4
      Max Yield: 6
```

**Expected Harvest Sequence**:
- Round 1: 7-10 Wildflower seeds
- Round 2: 5-7 Wildflower seeds
- Round 3: 2-4 Wildflower seeds
- Round 4: 3-7 random wildflower variety seeds

---

## Example 3: Sunflower Field

**Asset Name**: `Sunflower.asset`

**SeedData Settings**:
```
Seed Name:              Sunflower
Seed Icon:              [Sunflower sprite]
Growth Time:            75
Water Required:         35
Harvest Yield:          15
Max Harvests:           2
Oxygen Area Radius:     12

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Sunflower Seed
      Min Yield: 10
      Max Yield: 15
  [1] Flower Seed Name: Dwarf Sunflower Seed
      Min Yield: 8
      Max Yield: 12
```

**Expected Harvest Sequence**:
- Round 1: 12-18 Sunflower seeds
- Round 2: 8-15 random sunflower variety seeds

---

## Example 4: Lavender Bush

**Asset Name**: `Lavender.asset`

**SeedData Settings**:
```
Seed Name:              Lavender
Seed Icon:              [Lavender sprite]
Growth Time:            50
Water Required:         20
Harvest Yield:          6
Max Harvests:           5
Oxygen Area Radius:     7

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Lavender Seed
      Min Yield: 4
      Max Yield: 8
  [1] Flower Seed Name: Purple Lavender Seed
      Min Yield: 3
      Max Yield: 6
  [2] Flower Seed Name: White Lavender Seed
      Min Yield: 2
      Max Yield: 5
```

**Expected Harvest Sequence**:
- Round 1: 5-7 Lavender seeds
- Round 2: 3-5 Lavender seeds
- Round 3: 2-3 Lavender seeds
- Round 4: 2-3 Lavender seeds
- Round 5: 2-8 random lavender variety seeds

---

## Example 5: Tulip Garden (Low Yield)

**Asset Name**: `Tulip.asset`

**SeedData Settings**:
```
Seed Name:              Tulip
Seed Icon:              [Tulip sprite]
Growth Time:            55
Water Required:         28
Harvest Yield:          4
Max Harvests:           3
Oxygen Area Radius:     6

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Red Tulip Seed
      Min Yield: 5
      Max Yield: 8
  [1] Flower Seed Name: Yellow Tulip Seed
      Min Yield: 4
      Max Yield: 7
  [2] Flower Seed Name: Purple Tulip Seed
      Min Yield: 3
      Max Yield: 6
```

**Expected Harvest Sequence**:
- Round 1: 3-5 Tulip seeds
- Round 2: 2-4 Tulip seeds
- Round 3: 3-8 random tulip variety seeds

---

## Example 6: Non-Flower Tree (Traditional)

**Asset Name**: `OakTree.asset`

**SeedData Settings**:
```
Seed Name:              Acorn
Seed Icon:              [Acorn sprite]
Growth Time:            90
Water Required:         40
Harvest Yield:          5
Max Harvests:           3
Oxygen Area Radius:     15

Produces Flower Seeds:  FALSE
Flower Seed Yields:     (empty)
```

**Expected Harvest Sequence**:
- Round 1: 4-6 Acorn seeds
- Round 2: 2-4 Acorn seeds
- Round 3: 1-2 Acorn seeds
- Tree depletes (no flower seeds)

---

## Example 7: Single-Harvest Exotic Flower

**Asset Name**: `Hibiscus.asset`

**SeedData Settings**:
```
Seed Name:              Hibiscus
Seed Icon:              [Hibiscus sprite]
Growth Time:            65
Water Required:         32
Harvest Yield:          1
Max Harvests:           1
Oxygen Area Radius:     9

Produces Flower Seeds:  TRUE
Flower Seed Yields:
  [0] Flower Seed Name: Hibiscus Seed
      Min Yield: 8
      Max Yield: 12
  [1] Flower Seed Name: Red Hibiscus Seed
      Min Yield: 7
      Max Yield: 11
  [2] Flower Seed Name: Pink Hibiscus Seed
      Min Yield: 6
      Max Yield: 10
```

**Expected Harvest Sequence**:
- Round 1: 6-12 random hibiscus variety seeds
- Tree depletes

---

## Harvest Yield Calculations

Quick reference for calculating diminishing returns:

### Formula
```
Round N Yield = Base Yield × Multiplier[N] ± 20%
```

### Multipliers by Round
```
Round 0: 1.00 (100%)
Round 1: 0.65 (65%)
Round 2: 0.30 (30%)
Round 3+: 0.30 (30%)
```

### Examples

**Base Yield: 12 seeds**
- Round 1: 12 × 1.00 = 12 ± 20% = 10-14 seeds
- Round 2: 12 × 0.65 = 7.8 ? 8 ± 20% = 6-10 seeds
- Round 3: 12 × 0.30 = 3.6 ? 4 ± 20% = 3-5 seeds

**Base Yield: 8 seeds**
- Round 1: 8 × 1.00 = 8 ± 20% = 6-10 seeds
- Round 2: 8 × 0.65 = 5.2 ? 5 ± 20% = 4-6 seeds
- Round 3: 8 × 0.30 = 2.4 ? 2 ± 20% = 1-3 seeds

**Base Yield: 15 seeds**
- Round 1: 15 × 1.00 = 15 ± 20% = 12-18 seeds
- Round 2: 15 × 0.65 = 9.75 ? 10 ± 20% = 8-12 seeds
- Round 3: 15 × 0.30 = 4.5 ? 5 ± 20% = 4-6 seeds

---

## Configuration Tips

1. **For variety**: Add 2-4 different flower seed types per tree
2. **For rarity**: Use larger min/max ranges (e.g., 5-15)
3. **For consistency**: Use smaller min/max ranges (e.g., 6-8)
4. **For challenging**: Set higher max harvests (4-5) before flower seeds
5. **For quick harvesting**: Set max harvests to 1-2 for flower seed only trees

---

## Testing Each Configuration

To test:
1. Create or update the SeedData asset with the configuration
2. Place a SeedTree in the scene and assign the SeedData
3. Run the game and harvest the tree multiple times
4. Verify the sequence matches expectations
5. Check console logs for harvest details
6. Verify inventory shows correct seed types

Example console output:
```
[Cyan] Collecting seeds from 'Rose Bush' using SeedData: Rose x12
[Magenta] Adding seeds: Rose x10 (Round 1, Multiplier: 1.00)
[Green] Collected 10 Rose(s)! (Round 1/3)
...
[Cyan] Collecting flower seeds from 'Rose Bush': White Rose Seed x8
[Green] Collected 8 White Rose Seed(s)!
```
