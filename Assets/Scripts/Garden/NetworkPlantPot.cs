#if STEAM_BUILD
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Wraps an existing PlantPot for networked play. Sits alongside PlantPot on
/// a NetworkedPlantPot prefab variant (NetworkObject + this component).
///
/// HOST: PlantPot runs untouched (source of truth). This component mirrors
///       its state into NetworkVariables at SyncIntervalSeconds.
///
/// CLIENTS: PlantPot.enabled is set to false on spawn to kill the local Update
///          loop. This component applies visual-only updates from NetworkVariables
///          (scale lerp + seedling instantiation) using the same formula as PlantPot.
///
/// Interactions (water, plant, harvest) are routed through ServerRpc.
/// The OnMatured event on PlantPot (additive hook) triggers the networked
/// mature-plant handoff on the host.
/// </summary>
[RequireComponent(typeof(PlantPot))]
public class NetworkPlantPot : NetworkBehaviour
{
    [Header("Sync")]
    public float syncIntervalSeconds = 0.75f;

    private PlantPot pot;
    private float syncTimer;

    // NetworkVariables — host writes, clients read
    private NetworkVariable<FixedString64Bytes> netSeedName    = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float>              netGrowthTime  = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float>              netWaterLevel  = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool>               netIsPlanted   = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool>               netIsHarvest   = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameObject clientSeedling;
    private SeedData clientSeedData;

    private void Awake()
    {
        pot = GetComponent<PlantPot>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Kill local growth loop on clients — host is authoritative
            pot.enabled = false;
            netSeedName.OnValueChanged += OnClientSeedChanged;
            netGrowthTime.OnValueChanged += OnClientGrowthChanged;
            netIsPlanted.OnValueChanged += OnClientPlantedChanged;
        }
        else
        {
            // Subscribe to the additive PlantPot event for mature handoff
            pot.OnMatured += OnHostPlantMatured;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            pot.OnMatured -= OnHostPlantMatured;
        else
        {
            netSeedName.OnValueChanged -= OnClientSeedChanged;
            netGrowthTime.OnValueChanged -= OnClientGrowthChanged;
            netIsPlanted.OnValueChanged -= OnClientPlantedChanged;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        syncTimer += Time.deltaTime;
        if (syncTimer < syncIntervalSeconds) return;
        syncTimer = 0f;

        netSeedName.Value = pot.plantedSeedData != null ? pot.plantedSeedData.seedName : string.Empty;
        netGrowthTime.Value = pot.currentGrowthTime;
        netWaterLevel.Value = pot.currentWaterLevel;
        netIsPlanted.Value = pot.isPlanted;
        netIsHarvest.Value = pot.isReadyToHarvest;
    }

    // --- Host-side event: pot matured, swap to networked mature plant ---
    private void OnHostPlantMatured()
    {
        // PlantPot will destroy itself immediately after firing this event.
        // Spawn a NetworkObject mature-plant variant via NetworkFloraCatalog.
        var catalog = NetworkFloraCatalog.Instance;
        if (catalog == null || pot.plantedSeedData == null) return;

        var prefab = catalog.GetMaturePrefab(pot.plantedSeedData.seedName);
        if (prefab == null) return;

        var spawned = Instantiate(prefab, pot.plantSpawnPoint.position, Quaternion.identity);
        if (spawned.TryGetComponent<NetworkObject>(out var no))
            no.Spawn(destroyWithScene: true);
    }

    // --- Client visual updates ---
    private void OnClientSeedChanged(FixedString64Bytes prev, FixedString64Bytes next)
    {
        // Destroy old client seedling if seed changed
        if (clientSeedling != null)
        {
            Destroy(clientSeedling);
            clientSeedling = null;
            clientSeedData = null;
        }

        if (next.IsEmpty || SeedManager.Instance == null) return;

        clientSeedData = SeedManager.Instance.GetSeedDataByName(next.ToString());
        if (clientSeedData?.seedlingPrefab != null && pot.plantSpawnPoint != null)
        {
            clientSeedling = Instantiate(clientSeedData.seedlingPrefab,
                pot.plantSpawnPoint.position, Quaternion.identity, pot.plantSpawnPoint);
            clientSeedling.transform.localScale = Vector3.one * 0.3f;
        }
    }

    private void OnClientSeedChanged_Internal() { }

    private void OnClientPlantedChanged(bool prev, bool next)
    {
        if (!next && clientSeedling != null)
        {
            Destroy(clientSeedling);
            clientSeedling = null;
        }
    }

    private void OnClientGrowthChanged(float prev, float next)
    {
        if (clientSeedling == null || clientSeedData == null || clientSeedData.growthTime <= 0f) return;
        float percent = Mathf.Clamp01(next / clientSeedData.growthTime);
        float scale = Mathf.Lerp(0.3f, 1f, percent);
        clientSeedling.transform.localScale = Vector3.one * scale;
    }

    // --- RPCs: client → host interactions ---

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!ValidateOwner(rpcParams.Receive.SenderClientId)) return;
        pot.Interact();
    }

    [ServerRpc(RequireOwnership = false)]
    public void WaterServerRpc(float amount, ServerRpcParams rpcParams = default)
    {
        if (!ValidateOwner(rpcParams.Receive.SenderClientId)) return;
        pot.WaterPlant(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlantSeedServerRpc(FixedString64Bytes seedName, ServerRpcParams rpcParams = default)
    {
        if (!ValidateOwner(rpcParams.Receive.SenderClientId)) return;
        var seed = SeedManager.Instance?.GetSeedDataByName(seedName.ToString());
        if (seed != null) pot.PlantSeed(seed);
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakBranchServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!ValidateOwner(rpcParams.Receive.SenderClientId)) return;
        // Validate on server: branch break rules apply server-side
        if (GrowthStageUtil.CanBreakBranch(pot))
        {
            string branchItem = $"Branch:{pot.plantedSeedData?.seedName}";
            GrantItemClientRpc(branchItem, rpcParams.Receive.SenderClientId);

            float penalty = GraftConfig.Instance?.branchBreakPenaltySeconds ?? 30f;
            pot.currentGrowthTime = Mathf.Max(0f, pot.currentGrowthTime - penalty);
        }
    }

    [ClientRpc]
    private void GrantItemClientRpc(string itemName, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        InventoryManager.Instance?.AddItem(itemName, ItemType.Material, 1);
    }

    private bool ValidateOwner(ulong senderId)
    {
        // Only the plot owner can modify their own pots
        var registry = PlotRegistry.Instance;
        if (registry == null) return true; // No registry: allow (solo test)
        return registry.IsPlotOwner(senderId, gameObject);
    }
}
#endif
