using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps server-issued item IDs to client-side display data (name, icon, tier).
/// Odds and drop tables live on the server — this is cosmetics/display only.
/// Unknown IDs return a fallback entry so new items don't break the UI.
/// </summary>
[CreateAssetMenu(fileName = "GachaItemCatalog", menuName = "Arborvale/Gacha Item Catalog")]
public class GachaItemCatalog : ScriptableObject
{
    public static GachaItemCatalog Instance { get; private set; }

    public enum ItemTier { Common, Rare, FourStar }

    [Serializable]
    public struct Entry
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public ItemTier tier;
        public string seedName; // non-empty for common items: maps to SeedData in InventoryManager
    }

    public List<Entry> entries = new List<Entry>();
    public Sprite fallbackIcon;
    public string fallbackName = "Mystery Sapling";

    private Dictionary<string, Entry> lookup;

    private void OnEnable()
    {
        Instance = this;
        RebuildLookup();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void RebuildLookup()
    {
        lookup = new Dictionary<string, Entry>(entries.Count);
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.itemId))
                lookup[e.itemId] = e;
    }

    public Entry GetEntry(string itemId)
    {
        if (lookup == null) RebuildLookup();
        if (lookup.TryGetValue(itemId, out var entry)) return entry;
        return new Entry
        {
            itemId = itemId,
            displayName = fallbackName,
            icon = fallbackIcon,
            tier = ItemTier.Common
        };
    }

    public bool IsRareOrAbove(string itemId) =>
        GetEntry(itemId).tier >= ItemTier.Rare;
}
