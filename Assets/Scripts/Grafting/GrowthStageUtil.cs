using UnityEngine;

public enum GrowthStage
{
    Sprout,   // [0, sproutMax)  — too fragile
    Early,    // [sproutMax, earlyMax) — branch break allowed
    Late,     // [earlyMax, 1.0) — branch hardened
    Mature    // isReadyToHarvest
}

/// <summary>
/// Derives a named growth stage from PlantPot's public fields.
/// Thresholds come from GraftConfig so they're tunable without code changes.
/// PlantPot itself is not modified; this is a read-only adapter.
/// </summary>
public static class GrowthStageUtil
{
    public static GrowthStage GetStage(PlantPot pot)
    {
        if (pot == null || !pot.isPlanted || pot.plantedSeedData == null)
            return GrowthStage.Sprout;

        if (pot.isReadyToHarvest)
            return GrowthStage.Mature;

        float percent = Mathf.Clamp01(pot.currentGrowthTime / pot.plantedSeedData.growthTime);

        float sproutMax = GraftConfig.Instance != null ? GraftConfig.Instance.sproutMaxPercent : 0.15f;
        float earlyMax  = GraftConfig.Instance != null ? GraftConfig.Instance.earlyMaxPercent  : 0.50f;

        if (percent < sproutMax) return GrowthStage.Sprout;
        if (percent < earlyMax)  return GrowthStage.Early;
        return GrowthStage.Late;
    }

    public static bool CanBreakBranch(PlantPot pot) => GetStage(pot) == GrowthStage.Early;
}
