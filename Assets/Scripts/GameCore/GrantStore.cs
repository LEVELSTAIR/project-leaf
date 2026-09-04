using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks server-issued grant IDs for items in local inventory.
/// Used by TradeSession to distinguish tradeable online items (have grantId)
/// from non-tradeable demo/local items (no grantId).
///
/// FIFO per item name: oldest grant is used first when trading.
/// Grant IDs are never persisted locally — they live on the server.
/// A missing entry means the item was crafted offline and carries no server grant.
/// </summary>
public class GrantStore : MonoBehaviour
{
    public static GrantStore Instance { get; private set; }

    // itemName → queue of grantIds (oldest first)
    private readonly Dictionary<string, Queue<string>> grants =
        new Dictionary<string, Queue<string>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Registers a server grantId for an item entering the player's inventory.</summary>
    public void AddGrant(string itemName, string grantId)
    {
        if (string.IsNullOrEmpty(grantId)) return;
        if (!grants.TryGetValue(itemName, out var queue))
        {
            queue = new Queue<string>();
            grants[itemName] = queue;
        }
        queue.Enqueue(grantId);
    }

    /// <summary>
    /// Returns true and pops the oldest grantId if one exists for this item.
    /// Call only when the item is actually being consumed (traded / used).
    /// </summary>
    public bool TryConsumeGrant(string itemName, out string grantId)
    {
        grantId = null;
        if (!grants.TryGetValue(itemName, out var queue) || queue.Count == 0) return false;
        grantId = queue.Dequeue();
        if (queue.Count == 0) grants.Remove(itemName);
        return true;
    }

    /// <summary>Returns true if the player holds at least one server-granted copy of this item.</summary>
    public bool HasGrant(string itemName) =>
        grants.TryGetValue(itemName, out var q) && q.Count > 0;

    public int GrantCount(string itemName) =>
        grants.TryGetValue(itemName, out var q) ? q.Count : 0;
}
