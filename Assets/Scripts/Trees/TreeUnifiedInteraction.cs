using UnityEngine;

/// <summary>
/// Central controller for tree interactions.
/// Implements IInteractable and exposes a unified API for cutting, seed collection,
/// state queries, and tool‑based routing.
/// </summary>
[RequireComponent(typeof(TreeCuttable), typeof(SeedTree))]
public class TreeUnifiedInteraction : MonoBehaviour, IInteractable
{
    private TreeCuttable cuttable;
    private SeedTree seedTree;

    // ---- Unified state properties (from both components) ----
    public bool IsCutDown => cuttable != null && cuttable.IsCutDown;
    public int CurrentHits => cuttable != null ? cuttable.CurrentHits : 0;
    public int HitsToCut => cuttable != null ? cuttable.hitsToCut : 0;
    public float RespawnTimeRemaining => cuttable != null ? cuttable.RespawnTimeRemaining : 0f;

    public bool IsRegrowing => seedTree != null && seedTree.IsRegrowing;
    public float RegrowTimeRemaining => seedTree != null ? seedTree.RegrowTimeRemaining : 0f;
    public int HarvestCount => seedTree != null ? seedTree.HarvestCount : 0;
    public int RegrowCycleCount => seedTree != null ? seedTree.RegrowCycleCount : 0;
    public string SeedName => seedTree?.SeedData?.seedName ?? "Unknown Seed";
    public string TreeName => seedTree?.SeedData?.seedName ?? "Tree";

    public string StateDescription
    {
        get
        {
            if (IsCutDown)
                return RespawnTimeRemaining > 0 ? $"Stump (regrows in {RespawnTimeRemaining:F0}s)" : "Stump";
            if (IsRegrowing)
                return $"Regrowing ({RegrowTimeRemaining:F0}s remaining)";
            return $"Mature ({CurrentHits}/{HitsToCut} hits) – {SeedName} x{GetAvailableSeedAmount()}";
        }
    }

    // ---- IInteractable implementation ----
    public string InteractionPrompt
    {
        get
        {
            if (WeaponHolderController.Instance == null)
                return "Error: missing weapon controller";
            return GetPromptForTool(WeaponHolderController.Instance.CurrentTool);
        }
    }

    public void Interact()
    {
        if (WeaponHolderController.Instance == null) return;
        InteractWithTool(WeaponHolderController.Instance.CurrentTool);
    }

    public void Highlight(bool active)
    {
        seedTree?.Highlight(active && !IsCutDown);
    }

    // ---- Core actions ----
    public void CutTree() => cuttable?.CutTree();
    public void CollectSeeds() => seedTree?.CollectSeeds();

    // ---- Tool‑based routing ----
    public void InteractWithTool(string tool)
    {
        if (tool == "Axe")
            CutTree();
        else if (tool == "WateringCan")
            CollectSeeds();
        else
            HUDManager.Instance?.ShowMessage("Equip watering can (1) or axe (2)", 2f);
    }

    public string GetPromptForTool(string tool)
    {
        if (tool == "Axe")
            return cuttable?.GetInteractionPrompt() ?? "Cannot cut tree";
        if (tool == "WateringCan")
            return seedTree?.GetInteractionPrompt() ?? "Cannot collect seeds";
        return "Equip watering can (1) or axe (2)";
    }

    public int GetAvailableSeedAmount()
    {
        return seedTree != null ? seedTree.GetAdjustedSeedAmount() : 0;
    }

    // ---- UNIFIED STATE LOADING (used by SaveManager) ----
    public void LoadUnifiedState(UnifiedTreeState state)
    {
        // Restore cuttable state
        cuttable?.SetCuttableState(state.isCutDown, state.currentHits, state.respawnTimeRemaining);

        // Restore seed state
        seedTree?.SetSeedState(state.isRegrowing, state.harvestCount, state.regrowCycleCount, state.regrowTimeRemaining);
    }

    // ---- Optional: unified state object for saving ----
    public UnifiedTreeState GetUnifiedState()
    {
        return new UnifiedTreeState
        {
            isCutDown = IsCutDown,
            currentHits = CurrentHits,
            respawnTimeRemaining = RespawnTimeRemaining,
            isRegrowing = IsRegrowing,
            regrowTimeRemaining = RegrowTimeRemaining,
            harvestCount = HarvestCount,
            regrowCycleCount = RegrowCycleCount
        };
    }

    [System.Serializable]
    public struct UnifiedTreeState
    {
        public bool isCutDown;
        public int currentHits;
        public float respawnTimeRemaining;
        public bool isRegrowing;
        public float regrowTimeRemaining;
        public int harvestCount;
        public int regrowCycleCount;
    }

    // ---- MonoBehaviour ----
    private void Awake()
    {
        cuttable = GetComponent<TreeCuttable>();
        seedTree = GetComponent<SeedTree>();

        if (cuttable == null)
            Debug.LogError("TreeUnifiedInteraction: TreeCuttable missing!", this);
        if (seedTree == null)
            Debug.LogError("TreeUnifiedInteraction: SeedTree missing!", this);
    }
}