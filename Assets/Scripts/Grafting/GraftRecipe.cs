using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the combination rules and output for grafting two species together.
/// Recipes are order-insensitive on speciesA/B (the bench UI enforces this).
/// Online cost values override the demo fallbacks from GraftConfig.
/// </summary>
[CreateAssetMenu(fileName = "NewGraftRecipe", menuName = "Grafting/Graft Recipe")]
public class GraftRecipe : ScriptableObject
{
    [Header("Input Species (order-insensitive)")]
    public SeedData speciesA;
    public SeedData speciesB;

    [Header("Output Parts")]
    [Tooltip("Trunk part used for the hybrid. Typically from speciesA.")]
    public PlantPartDef resultTrunkPart;

    [Tooltip("Foliage part used for the hybrid. Typically from speciesB.")]
    public PlantPartDef resultFoliagePart;

    [Tooltip("Valid bloom parts for this recipe (player picks one or first is used).")]
    public List<PlantPartDef> allowedBloomParts = new List<PlantPartDef>();

    [Header("Timing")]
    [Tooltip("Time in seconds the graft bench takes to process.")]
    public float graftTimeSeconds = 120f;

    [Header("Demo / Offline Cost Overrides")]
    [Tooltip("Leave blank to use GraftConfig defaults.")]
    public string demoFertilizerOverrideName;
    public int demoFertilizerOverrideAmount = 0;

    [Header("Output Seed")]
    [Tooltip("The SeedData that PlantPot uses to grow the resulting hybrid. " +
             "Its seedlingPrefab/maturePlantPrefab must point at assembled HybridPlantRoot prefabs.")]
    public SeedData resultSeedData;

    /// <summary>True if this recipe matches the two given species (order-insensitive).</summary>
    public bool Matches(SeedData a, SeedData b)
    {
        return (speciesA == a && speciesB == b) || (speciesA == b && speciesB == a);
    }

    public string GetFertilizerItemName()
    {
        return !string.IsNullOrEmpty(demoFertilizerOverrideName)
            ? demoFertilizerOverrideName
            : GraftConfig.Instance?.demoFertilizerItemName ?? "Fertilizer";
    }

    public int GetFertilizerAmount()
    {
        return demoFertilizerOverrideAmount > 0
            ? demoFertilizerOverrideAmount
            : GraftConfig.Instance?.demoFertilizerAmount ?? 3;
    }
}
