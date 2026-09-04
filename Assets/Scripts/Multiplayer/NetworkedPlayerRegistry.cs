#if STEAM_BUILD
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Singleton NetworkBehaviour that maps NGO clientId → player Transform.
/// Lives on a NetworkObject in Eden_Shared.unity.
/// Each spawned player prefab calls RegisterLocalPlayer via ServerRpc on spawn.
/// TradeSession reads this to enforce the 6m proximity requirement.
/// </summary>
public class NetworkedPlayerRegistry : NetworkBehaviour
{
    public static NetworkedPlayerRegistry Instance { get; private set; }

    // Host-only: authoritative map populated by ServerRpc registrations.
    private readonly Dictionary<ulong, Transform> playerTransforms = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerRegistry] Duplicate instance detected — destroying extra.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Called by each player's FirstPersonController (or player prefab root) during OnNetworkSpawn.
    /// The player passes its own Transform so the host can track positions for trade distance checks.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        // We can't serialize Transform directly; we store the sender's NetworkObjectId
        // and resolve it to a position at check-time. Instead, let the host look up by
        // clientId from connected client list.
        // Actual transform is looked up from the player NetworkObject at call time.
        // Placeholder — GetPlayerTransform does the live lookup.
        Debug.Log($"[PlayerRegistry] Player registered: clientId={clientId}");
    }

    /// <summary>
    /// Registers a local Transform reference (host-side direct call).
    /// Called when a player NetworkObject spawns on the host.
    /// </summary>
    public void RegisterLocalTransform(ulong clientId, Transform t)
    {
        playerTransforms[clientId] = t;
    }

    public void UnregisterPlayer(ulong clientId) => playerTransforms.Remove(clientId);

    /// <returns>True if both transforms found and position retrieved.</returns>
    public bool TryGetPosition(ulong clientId, out Vector3 position)
    {
        if (playerTransforms.TryGetValue(clientId, out var t) && t != null)
        {
            position = t.position;
            return true;
        }
        position = Vector3.zero;
        return false;
    }
}
#endif
