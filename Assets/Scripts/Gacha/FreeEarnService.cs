#if STEAM_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Arborvale.Shared;
using UnityEngine;

/// <summary>
/// Tracks free-currency milestones (daily login, harvest counts) and posts them
/// to /v1/telemetry/events. The server decides what awards to grant — the client
/// never computes award amounts. After posting, a fresh /v1/wallet fetch updates
/// the HUD balance so the player sees any credited awards immediately.
///
/// Milestone check fires at session start and on PlantPot harvest events.
/// </summary>
public class FreeEarnService : MonoBehaviour
{
    public static FreeEarnService Instance { get; private set; }

    private const string LastLoginKey = "FreeEarn_LastLoginDate";
    private const string HarvestCountKey = "FreeEarn_HarvestCount";

    private readonly List<TelemetryEventDto> pendingEvents = new List<TelemetryEventDto>();
    private bool flushInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!OnlineServices.IsAvailable) return;
        CheckDailyLogin();
    }

    // ── Milestone: daily login ─────────────────────────────────────────

    private void CheckDailyLogin()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string last = PlayerPrefs.GetString(LastLoginKey, string.Empty);

        if (last == today) return;

        PlayerPrefs.SetString(LastLoginKey, today);
        PlayerPrefs.Save();

        QueueEvent("daily_login", $"{{\"date\":\"{today}\"}}");
        StartCoroutine(FlushAndRefreshWallet());
    }

    // ── Milestone: harvest ─────────────────────────────────────────────

    /// <summary>Call when any plant is harvested. Tracks cumulative count and sends milestones.</summary>
    public void OnPlantHarvested(string seedName)
    {
        int count = PlayerPrefs.GetInt(HarvestCountKey, 0) + 1;
        PlayerPrefs.SetInt(HarvestCountKey, count);
        PlayerPrefs.Save();

        QueueEvent("harvest", $"{{\"seedName\":\"{seedName}\",\"totalCount\":{count}}}");

        // Flush at every 10th harvest to avoid per-harvest HTTP calls
        if (count % 10 == 0)
            StartCoroutine(FlushAndRefreshWallet());
    }

    // ── Flush ──────────────────────────────────────────────────────────

    private void QueueEvent(string type, string payload)
    {
        pendingEvents.Add(new TelemetryEventDto(
            type,
            payload,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    private IEnumerator FlushAndRefreshWallet()
    {
        if (flushInProgress || pendingEvents.Count == 0 || !OnlineServices.IsAvailable) yield break;
        flushInProgress = true;

        var batch = new TelemetryBatchDto(pendingEvents.ToArray());
        pendingEvents.Clear();

        var postTask = OnlineServices.Instance.PostTelemetryAsync(batch);
        while (!postTask.IsCompleted) yield return null;

        if (postTask.IsFaulted)
        {
            Debug.LogWarning($"[FreeEarnService] Telemetry post failed: {postTask.Exception?.GetBaseException().Message}");
            // Re-queue events so they're sent next time
            if (batch.Events != null)
                pendingEvents.InsertRange(0, batch.Events);
        }
        else
        {
            // Refresh wallet to pick up any server-awarded BloomShards
            var walletTask = OnlineServices.Instance.GetWalletAsync();
            while (!walletTask.IsCompleted) yield return null;

            if (!walletTask.IsFaulted && walletTask.Result != null)
                WalletWidget.Instance?.Refresh(walletTask.Result);
        }

        flushInProgress = false;
    }

    private void OnApplicationQuit()
    {
        // Best-effort flush on quit — fire-and-forget
        if (pendingEvents.Count > 0 && OnlineServices.IsAvailable)
            StartCoroutine(FlushAndRefreshWallet());
    }
}
#endif
