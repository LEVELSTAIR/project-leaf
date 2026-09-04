#if STEAM_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-only service that persists plot claims and pot states to a per-lobby JSON file.
/// On re-host the same lobby owner restores the world state.
/// Economy-critical data (grants, balances) is never stored here — it lives server-side.
/// </summary>
public class WorldSaveService : MonoBehaviour
{
    public static WorldSaveService Instance { get; private set; }

    [Header("Save Settings")]
    public float autoSaveIntervalSeconds = 60f;

    [Header("References")]
    public GameObject networkedPotPrefab;
    public PlotMarker[] plotMarkers;

    private float saveTimer;
    private string savePath;
    private bool isHost;

    // ──────────────────────────────────── Serialisable data shapes ────

    [Serializable]
    private class PotSaveData
    {
        public int plotId;
        public string seedName;
        public float currentGrowthTime;
        public float currentWaterLevel;
        public bool isReadyToHarvest;
    }

    [Serializable]
    private class ClaimSaveData
    {
        public int plotId;
        public ulong ownerSteamId;
    }

    [Serializable]
    private class WorldSaveData
    {
        public string lobbyOwnerSteamId;
        public List<ClaimSaveData> claims = new List<ClaimSaveData>();
        public List<PotSaveData> pots = new List<PotSaveData>();
    }

    // ──────────────────────────────────── Lifecycle ────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsHost) return;

        isHost = true;
        string steamIdStr = SteamUser.GetSteamID().m_SteamID.ToString();
        string dir = Path.Combine(Application.persistentDataPath, "worlds");
        Directory.CreateDirectory(dir);
        savePath = Path.Combine(dir, $"{steamIdStr}.json");

        TryRestore();
    }

    private void Update()
    {
        if (!isHost) return;
        saveTimer += Time.deltaTime;
        if (saveTimer >= autoSaveIntervalSeconds)
        {
            saveTimer = 0f;
            SaveWorld();
        }
    }

    private void OnApplicationQuit()
    {
        if (isHost) SaveWorld();
    }

    // ──────────────────────────────────── Save ────

    public void SaveWorld()
    {
        if (!isHost || PlotRegistry.Instance == null) return;

        var data = new WorldSaveData
        {
            lobbyOwnerSteamId = SteamUser.GetSteamID().m_SteamID.ToString()
        };

        foreach (var c in PlotRegistry.Instance.GetAllClaims())
            data.claims.Add(new ClaimSaveData { plotId = c.plotId, ownerSteamId = c.ownerSteamId });

        foreach (var netPot in FindObjectsByType<NetworkPlantPot>(FindObjectsSortMode.None))
        {
            var ownership = netPot.GetComponent<PlotOwnership>();
            var pot = netPot.GetComponent<PlantPot>();
            if (pot == null || ownership == null) continue;

            data.pots.Add(new PotSaveData
            {
                plotId = ownership.plotId,
                seedName = pot.plantedSeedData?.seedName ?? string.Empty,
                currentGrowthTime = pot.currentGrowthTime,
                currentWaterLevel = pot.currentWaterLevel,
                isReadyToHarvest = pot.isReadyToHarvest
            });
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(data, prettyPrint: true));
        Debug.Log($"[WorldSaveService] Saved to {savePath}");
    }

    // ──────────────────────────────────── Restore ────

    private void TryRestore()
    {
        if (!File.Exists(savePath)) return;

        WorldSaveData data;
        try { data = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(savePath)); }
        catch (Exception e)
        {
            Debug.LogWarning($"[WorldSaveService] Cannot parse save: {e.Message}");
            return;
        }

        StartCoroutine(RestoreCoroutine(data));
    }

    private IEnumerator RestoreCoroutine(WorldSaveData data)
    {
        // Wait for PlotRegistry to spawn on the network
        yield return new WaitUntil(() => PlotRegistry.Instance != null && PlotRegistry.Instance.IsSpawned);

        // Restore claims — client IDs are not stable across sessions, so we use SteamId as the
        // stable key and remap when the player actually connects (LobbyService hooks into this).
        var restoredClaims = new List<PlotClaim>();
        foreach (var c in data.claims)
        {
            restoredClaims.Add(new PlotClaim
            {
                plotId = c.plotId,
                ownerClientId = NetworkManager.ServerClientId, // placeholder until player reconnects
                ownerSteamId = c.ownerSteamId
            });
        }
        PlotRegistry.Instance.RestoreClaims(restoredClaims);

        if (networkedPotPrefab == null) yield break;

        foreach (var potData in data.pots)
        {
            var marker = FindMarker(potData.plotId);
            if (marker == null) continue;

            var go = Instantiate(networkedPotPrefab, marker.transform.position, marker.transform.rotation);
            var no = go.GetComponent<NetworkObject>();
            no?.Spawn(destroyWithScene: true);

            var ownership = go.GetComponent<PlotOwnership>();
            if (ownership != null) ownership.plotId = potData.plotId;

            if (!string.IsNullOrEmpty(potData.seedName))
            {
                var seed = SeedManager.Instance?.GetSeedData(potData.seedName);
                var pot = go.GetComponent<PlantPot>();
                if (pot != null && seed != null)
                {
                    pot.PlantSeed(seed);
                    pot.currentGrowthTime = potData.currentGrowthTime;
                    pot.currentWaterLevel = potData.currentWaterLevel;
                }
            }

            yield return null; // stagger spawns
        }

        Debug.Log("[WorldSaveService] World restored.");
    }

    private PlotMarker FindMarker(int plotId)
    {
        if (plotMarkers != null)
            foreach (var m in plotMarkers)
                if (m != null && m.plotId == plotId) return m;
        return null;
    }
}
#endif
