#if STEAM_BUILD
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct PlotClaim : INetworkSerializable, IEquatable<PlotClaim>
{
    public int plotId;
    public ulong ownerClientId;
    public ulong ownerSteamId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref plotId);
        serializer.SerializeValue(ref ownerClientId);
        serializer.SerializeValue(ref ownerSteamId);
    }

    public bool Equals(PlotClaim other) =>
        plotId == other.plotId && ownerClientId == other.ownerClientId;
}

/// <summary>
/// Host-authoritative registry of plot claims. NetworkList replicates to all clients.
/// Placed in Eden_Shared.unity alongside NetworkManager.
/// One claim per player; plot must be unclaimed to claim it.
/// </summary>
public class PlotRegistry : NetworkBehaviour
{
    public static PlotRegistry Instance { get; private set; }

    public event Action<NetworkListEvent<PlotClaim>> OnClaimsChanged;

    private NetworkList<PlotClaim> claims;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        claims = new NetworkList<PlotClaim>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        claims.OnListChanged += ForwardClaimsChanged;
        if (IsServer)
            Arborvale.Shared.NetworkSessionEvents.ClientSteamIdResolved += RemapClientId;
    }

    public override void OnNetworkDespawn()
    {
        claims.OnListChanged -= ForwardClaimsChanged;
        if (IsServer)
            Arborvale.Shared.NetworkSessionEvents.ClientSteamIdResolved -= RemapClientId;
    }

    private void ForwardClaimsChanged(NetworkListEvent<PlotClaim> e) => OnClaimsChanged?.Invoke(e);

    public bool IsPlotClaimed(int plotId) => FindClaim(plotId).HasValue;

    public PlotClaim? GetClaim(int plotId) => FindClaim(plotId);

    /// <summary>
    /// Returns true if clientId owns the plot that obj belongs to.
    /// Returns true when obj has no PlotOwnership (unassigned pots: allow interaction).
    /// </summary>
    public bool IsPlotOwner(ulong clientId, GameObject obj)
    {
        var ownership = obj.GetComponent<PlotOwnership>();
        if (ownership == null || ownership.plotId < 0) return true;
        var claim = FindClaim(ownership.plotId);
        return claim.HasValue && claim.Value.ownerClientId == clientId;
    }

    public List<PlotClaim> GetAllClaims()
    {
        var result = new List<PlotClaim>(claims.Count);
        foreach (var c in claims) result.Add(c);
        return result;
    }

    /// <summary>Called by WorldSaveService to restore a previous session's claims.</summary>
    public void RestoreClaims(List<PlotClaim> saved)
    {
        if (!IsServer) return;
        claims.Clear();
        foreach (var c in saved) claims.Add(c);
    }

    /// <summary>Update clientId mapping when a returning player rejoins (matched by SteamId).</summary>
    public void RemapClientId(ulong steamId, ulong newClientId)
    {
        if (!IsServer) return;
        for (int i = 0; i < claims.Count; i++)
        {
            if (claims[i].ownerSteamId == steamId)
            {
                var updated = claims[i];
                updated.ownerClientId = newClientId;
                claims[i] = updated;
                return;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClaimPlotServerRpc(int plotId, ulong steamId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // One plot per player
        foreach (var c in claims)
            if (c.ownerClientId == clientId) return;

        // Plot must be unclaimed
        if (IsPlotClaimed(plotId)) return;

        claims.Add(new PlotClaim
        {
            plotId = plotId,
            ownerClientId = clientId,
            ownerSteamId = steamId
        });
    }

    private PlotClaim? FindClaim(int plotId)
    {
        foreach (var c in claims)
            if (c.plotId == plotId) return c;
        return null;
    }
}
#endif
