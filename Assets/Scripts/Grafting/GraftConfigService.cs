using System.Collections;
using System.Collections.Generic;
using Arborvale.Shared;
using UnityEngine;

/// <summary>
/// Fetches and caches server-authoritative graft costs from /v1/config/graft.
/// Falls back silently to GraftRecipe demo values when offline or fetch fails.
/// Attach to a persistent GameObject in the main scene.
/// </summary>
public class GraftConfigService : MonoBehaviour
{
    public static GraftConfigService Instance { get; private set; }

    private GraftConfigDto cachedConfig;
    private bool fetched;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (OnlineServices.IsAvailable)
            StartCoroutine(FetchRoutine());
    }

    /// <summary>
    /// Returns server-provided cost for the species pair, or null if offline/not loaded.
    /// GraftingBench falls back to recipe demo values when this returns null.
    /// </summary>
    public GraftCostDto GetCost(string speciesA, string speciesB)
    {
        if (cachedConfig?.Costs == null) return null;
        foreach (var cost in cachedConfig.Costs)
        {
            bool fwd = cost.SpeciesA == speciesA && cost.SpeciesB == speciesB;
            bool rev = cost.SpeciesA == speciesB && cost.SpeciesB == speciesA;
            if (fwd || rev) return cost;
        }
        return null;
    }

    public void Invalidate() => fetched = false;

    private IEnumerator FetchRoutine()
    {
        if (fetched || !OnlineServices.IsAvailable) yield break;

        var task = OnlineServices.Instance.GetGraftConfigAsync();
        while (!task.IsCompleted) yield return null;

        if (!task.IsFaulted)
        {
            cachedConfig = task.Result;
            fetched = true;
            Debug.Log($"[GraftConfigService] Loaded {cachedConfig?.Costs?.Length ?? 0} graft cost entries.");
        }
        else
        {
            Debug.LogWarning($"[GraftConfigService] Fetch failed: {task.Exception?.GetBaseException().Message}");
        }
    }
}
