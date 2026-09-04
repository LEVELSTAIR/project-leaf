using UnityEngine;

/// <summary>
/// Gameplay-pacing config for the grafting system (open repo — not economy-critical).
/// Online cost/success values are fetched from the backend and override demo fallbacks.
/// </summary>
[CreateAssetMenu(fileName = "GraftConfig", menuName = "Grafting/Graft Config")]
public class GraftConfig : ScriptableObject
{
    public static GraftConfig Instance { get; private set; }

    [Header("Growth Stage Thresholds")]
    [Tooltip("Below this percent, the plant is too fragile to break a branch.")]
    [Range(0f, 1f)] public float sproutMaxPercent = 0.15f;

    [Tooltip("Branch breaking is allowed between sproutMax and earlyMax.")]
    [Range(0f, 1f)] public float earlyMaxPercent = 0.50f;

    [Tooltip("Seconds of growth time removed from a pot when a branch is broken.")]
    public float branchBreakPenaltySeconds = 30f;

    [Header("Demo / Offline Fallback Costs")]
    [Tooltip("Used in solo/demo builds where the backend is unavailable.")]
    public string demoFertilizerItemName = "Fertilizer";
    public int demoFertilizerAmount = 3;
    public int demoCurrencyCost = 0;

    [Header("Demo Success Chance")]
    [Tooltip("Probability [0,1] of a successful graft in offline/demo mode.")]
    [Range(0f, 1f)] public float demoSuccessChance = 0.75f;

    private void OnEnable()
    {
        Instance = this;
    }
}
