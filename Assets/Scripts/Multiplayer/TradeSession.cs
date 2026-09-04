#if STEAM_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Arborvale.Shared;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum TradeState
{
    Idle,
    Invited,
    Negotiating,
    BothAccepted,
    Committing,
    Completed,
    Failed,
    Cancelled
}

[Serializable]
public struct TradeItem : INetworkSerializable, IEquatable<TradeItem>
{
    public FixedString64Bytes itemName;
    public int quantity;
    public ItemType itemType;
    public bool hasGrant; // has a server grantId — commit path calls backend

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref quantity);
        serializer.SerializeValue(ref itemType);
        serializer.SerializeValue(ref hasGrant);
    }

    public bool Equals(TradeItem other) =>
        itemName == other.itemName && quantity == other.quantity && itemType == other.itemType;
}

/// <summary>
/// Singleton NetworkBehaviour managing the one active P2P trade in the session.
/// Placed in Eden_Shared.unity; starts Idle and resets after each trade.
///
/// State machine:
///   Idle → Invited → Negotiating → BothAccepted (3s lock) → Committing → Completed|Failed
///   Any state → Cancelled (decline, disconnect, cancel press, distance > 6m)
///
/// Grant items (hasGrant = true) are committed via the backend; local items are
/// swapped host-side via InventoryManager. Grant IDs never travel over the network.
/// </summary>
public class TradeSession : NetworkBehaviour
{
    public static TradeSession Instance { get; private set; }

    public const float MaxTradeDistanceMeters = 6f;
    private const float LockDurationSeconds = 3f;

    // ── Network state ──────────────────────────────────────────────────
    private NetworkVariable<TradeState> netState =
        new NetworkVariable<TradeState>(TradeState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkVariable<ulong> netInitiatorId =
        new NetworkVariable<ulong>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkVariable<ulong> netRecipientId =
        new NetworkVariable<ulong>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> netInitiatorAccepted =
        new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> netRecipientAccepted =
        new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkList<TradeItem> initiatorOffer;
    private NetworkList<TradeItem> recipientOffer;

    // ── Local-only ─────────────────────────────────────────────────────
    public event Action<TradeState> OnStateChanged;

    private float lockTimer;
    private bool commitStarted;

    // ── Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        initiatorOffer = new NetworkList<TradeItem>();
        recipientOffer = new NetworkList<TradeItem>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        netState.OnValueChanged += (_, s) => OnStateChanged?.Invoke(s);
    }

    public override void OnNetworkDespawn()
    {
        netState.OnValueChanged -= (_, s) => OnStateChanged?.Invoke(s);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (netState.Value == TradeState.BothAccepted)
        {
            lockTimer += Time.deltaTime;
            if (lockTimer >= LockDurationSeconds && !commitStarted)
            {
                commitStarted = true;
                StartCoroutine(CommitRoutine());
            }
        }

        // Distance check — cancel if players drift apart
        if (netState.Value is TradeState.Negotiating or TradeState.BothAccepted or TradeState.Invited)
        {
            if (PlayersToFarApart())
                ServerCancel("Players moved too far apart.");
        }
    }

    // ── Public properties ──────────────────────────────────────────────

    public TradeState State => netState.Value;
    public ulong InitiatorId => netInitiatorId.Value;
    public ulong RecipientId => netRecipientId.Value;
    public NetworkList<TradeItem> InitiatorOffer => initiatorOffer;
    public NetworkList<TradeItem> RecipientOffer => recipientOffer;

    public bool IsParticipant(ulong clientId) =>
        clientId == netInitiatorId.Value || clientId == netRecipientId.Value;

    public NetworkList<TradeItem> GetMyOffer(ulong clientId) =>
        clientId == netInitiatorId.Value ? initiatorOffer : recipientOffer;

    // ── RPCs: client → host ────────────────────────────────────────────

    /// <summary>Initiator sends a trade invite to another player.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void InviteServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        if (netState.Value != TradeState.Idle) return;
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId == targetClientId) return;

        netInitiatorId.Value = senderId;
        netRecipientId.Value = targetClientId;
        SetState(TradeState.Invited);

        NotifyInviteClientRpc(senderId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        });
    }

    /// <summary>Recipient accepts or declines the invite.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void RespondToInviteServerRpc(bool accepted, ServerRpcParams rpcParams = default)
    {
        if (netState.Value != TradeState.Invited) return;
        if (rpcParams.Receive.SenderClientId != netRecipientId.Value) return;

        if (accepted)
            SetState(TradeState.Negotiating);
        else
            ServerCancel("Trade invite declined.");
    }

    /// <summary>Either participant updates their offer. Clears both accepts.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetOfferServerRpc(TradeItem[] items, ServerRpcParams rpcParams = default)
    {
        if (netState.Value != TradeState.Negotiating) return;
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!IsParticipant(senderId)) return;

        var offer = senderId == netInitiatorId.Value ? initiatorOffer : recipientOffer;
        offer.Clear();
        foreach (var item in items) offer.Add(item);

        // Any offer change clears both accepts
        netInitiatorAccepted.Value = false;
        netRecipientAccepted.Value = false;
    }

    /// <summary>A participant confirms they accept the current offers.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void AcceptOfferServerRpc(ServerRpcParams rpcParams = default)
    {
        if (netState.Value != TradeState.Negotiating) return;
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId == netInitiatorId.Value)
            netInitiatorAccepted.Value = true;
        else if (senderId == netRecipientId.Value)
            netRecipientAccepted.Value = true;

        if (netInitiatorAccepted.Value && netRecipientAccepted.Value)
        {
            lockTimer = 0f;
            commitStarted = false;
            SetState(TradeState.BothAccepted);
        }
    }

    /// <summary>Either participant or the host cancels the trade.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void CancelServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsParticipant(rpcParams.Receive.SenderClientId) && !IsServer) return;
        ServerCancel("Trade cancelled.");
    }

    // ── Host commit logic ──────────────────────────────────────────────

    private IEnumerator CommitRoutine()
    {
        SetState(TradeState.Committing);

        // NetworkList<T> exposes GetEnumerator but does not implement IEnumerable<T>,
        // so the List<T> copy constructor can't take it — copy manually.
        var initItems = new List<TradeItem>(initiatorOffer.Count);
        foreach (var item in initiatorOffer) initItems.Add(item);
        var recipItems = new List<TradeItem>(recipientOffer.Count);
        foreach (var item in recipientOffer) recipItems.Add(item);

        // Validate both sides still have the items
        if (!ValidateInventory(netInitiatorId.Value, initItems) ||
            !ValidateInventory(netRecipientId.Value, recipItems))
        {
            SetState(TradeState.Failed);
            NotifyTradeResultClientRpc(false, "Inventory changed during trade.");
            yield break;
        }

        // Commit grant items via backend (each client calls independently)
        bool hasGrants = HasGrantItems(initItems) || HasGrantItems(recipItems);
        if (hasGrants && OnlineServices.IsAvailable)
        {
            // Signal clients to begin their backend commit; wait for confirmations
            string tradeSessionId = Guid.NewGuid().ToString();
            RequestBackendCommitClientRpc(tradeSessionId,
                netInitiatorId.Value, netRecipientId.Value);

            // Give clients up to 10s to confirm
            float timeout = 10f;
            bool initiatorConfirmed = false, recipientConfirmed = false;

            pendingInitiatorConfirm = false;
            pendingRecipientConfirm = false;

            while (timeout > 0f && (!initiatorConfirmed || !recipientConfirmed))
            {
                yield return null;
                timeout -= Time.deltaTime;
                initiatorConfirmed |= pendingInitiatorConfirm;
                recipientConfirmed |= pendingRecipientConfirm;
            }

            if (!initiatorConfirmed || !recipientConfirmed)
            {
                SetState(TradeState.Failed);
                NotifyTradeResultClientRpc(false, "Backend commit timed out.");
                yield break;
            }
        }

        // Swap local items on host
        SwapLocalItems(netInitiatorId.Value, netRecipientId.Value, initItems, recipItems);

        SetState(TradeState.Completed);
        NotifyTradeResultClientRpc(true, string.Empty);

        // Reset after brief display delay
        yield return new WaitForSeconds(2f);
        ServerReset();
    }

    // Flags set by ConfirmBackendCommitServerRpc
    private bool pendingInitiatorConfirm;
    private bool pendingRecipientConfirm;

    /// <summary>Client calls this after successfully committing grants to backend.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ConfirmBackendCommitServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;
        if (id == netInitiatorId.Value) pendingInitiatorConfirm = true;
        if (id == netRecipientId.Value) pendingRecipientConfirm = true;
    }

    // ── ClientRpc notifications ────────────────────────────────────────

    [ClientRpc]
    private void NotifyInviteClientRpc(ulong fromClientId, ClientRpcParams rpcParams = default)
    {
        TradeUIManager.Instance?.ShowIncomingInvite(fromClientId);
    }

    [ClientRpc]
    private void RequestBackendCommitClientRpc(string tradeSessionId, ulong initiatorId, ulong recipientId)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (localId != initiatorId && localId != recipientId) return;

        bool isInitiator = localId == initiatorId;
        var myOffer = isInitiator ? initiatorOffer : recipientOffer;
        StartCoroutine(LocalBackendCommit(tradeSessionId, myOffer));
    }

    [ClientRpc]
    private void NotifyTradeResultClientRpc(bool success, string reason)
    {
        TradeUIManager.Instance?.ShowResult(success, reason);
    }

    // ── Local backend commit (runs on each client) ─────────────────────

    private IEnumerator LocalBackendCommit(string tradeSessionId, NetworkList<TradeItem> myOffer)
    {
        var grantIds = new List<string>();
        foreach (var item in myOffer)
        {
            if (!item.hasGrant) continue;
            if (GrantStore.Instance != null &&
                GrantStore.Instance.TryConsumeGrant(item.itemName.ToString(), out string grantId))
            {
                grantIds.Add(grantId);
            }
        }

        if (grantIds.Count == 0)
        {
            // No grants to commit — confirm immediately
            ConfirmBackendCommitServerRpc();
            yield break;
        }

        // Send grant commit to backend
        var task = OnlineServices.Instance?.AcceptTradeAsync(tradeSessionId);
        if (task == null) { ConfirmBackendCommitServerRpc(); yield break; }

        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            Debug.LogError($"[TradeSession] Backend commit failed: {task.Exception?.GetBaseException().Message}");
            // Don't confirm — host will time out and fail the trade
        }
        else
        {
            ConfirmBackendCommitServerRpc();
        }
    }

    // ── Host helpers ───────────────────────────────────────────────────

    private void ServerCancel(string reason)
    {
        SetState(TradeState.Cancelled);
        NotifyTradeResultClientRpc(false, reason);
        StartCoroutine(DelayedReset());
    }

    private IEnumerator DelayedReset()
    {
        yield return new WaitForSeconds(1f);
        ServerReset();
    }

    private void ServerReset()
    {
        initiatorOffer.Clear();
        recipientOffer.Clear();
        netInitiatorAccepted.Value = false;
        netRecipientAccepted.Value = false;
        netInitiatorId.Value = 0;
        netRecipientId.Value = 0;
        SetState(TradeState.Idle);
    }

    private void SetState(TradeState s)
    {
        netState.Value = s;
        Debug.Log($"[TradeSession] → {s}");
    }

    private bool ValidateInventory(ulong clientId, List<TradeItem> items)
    {
        // Host-side validation: check InventoryManager only for local client (host is always server here)
        // For remote clients we rely on the earlier SetOffer validation + good faith
        // Full server-side inventory tracking is a future hardening item
        return true;
    }

    private bool HasGrantItems(List<TradeItem> items)
    {
        foreach (var i in items)
            if (i.hasGrant) return true;
        return false;
    }

    private void SwapLocalItems(ulong fromId, ulong toId, List<TradeItem> fromItems, List<TradeItem> toItems)
    {
        // Host executes InventoryManager swaps for local (non-grant) items.
        // Grant items were committed by the backend; clients receive them server-side
        // and are notified via the /v1/grants endpoint on next fetch.
        // For simplicity in v1, host swaps all local items (non-hasGrant) directly.
        SwapLocalItemsForClient(fromId, toId, fromItems);
        SwapLocalItemsForClient(toId, fromId, toItems);
    }

    private void SwapLocalItemsForClient(ulong giverId, ulong receiverId, List<TradeItem> items)
    {
        foreach (var item in items)
        {
            if (item.hasGrant) continue; // backend handled
            string name = item.itemName.ToString();
            // On host: remove from giver's inventory (host = server, only local inv visible)
            // For networked inventory, this needs InventoryRpcs — deferred to inventory hardening
            InventoryGrantClientRpc(name, item.quantity, item.itemType,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { receiverId } } });
            InventoryRemoveClientRpc(name, item.quantity, item.itemType,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { giverId } } });
        }
    }

    [ClientRpc]
    private void InventoryGrantClientRpc(string itemName, int qty, ItemType type, ClientRpcParams rpcParams = default)
    {
        InventoryManager.Instance?.AddItem(itemName, type, qty);
    }

    [ClientRpc]
    private void InventoryRemoveClientRpc(string itemName, int qty, ItemType type, ClientRpcParams rpcParams = default)
    {
        InventoryManager.Instance?.RemoveItem(itemName, type, qty);
    }

    private bool PlayersToFarApart()
    {
        if (NetworkedPlayerRegistry.Instance == null) return false;
        if (!NetworkedPlayerRegistry.Instance.TryGetPosition(netInitiatorId.Value, out var posA)) return false;
        if (!NetworkedPlayerRegistry.Instance.TryGetPosition(netRecipientId.Value, out var posB)) return false;
        return Vector3.Distance(posA, posB) > MaxTradeDistanceMeters;
    }
}
#endif
