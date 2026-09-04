using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps seed names to their networked mature-plant prefabs (must carry NetworkObject).
/// Assign in Inspector; add entries for every plantable species including hybrids.
/// </summary>
[CreateAssetMenu(fileName = "NetworkFloraCatalog", menuName = "Arborvale/Network Flora Catalog")]
public class NetworkFloraCatalog : ScriptableObject
{
    public static NetworkFloraCatalog Instance { get; private set; }

    [System.Serializable]
    public struct Entry
    {
        public string seedName;
        public GameObject maturePrefab;
    }

    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, GameObject> lookup;

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
        lookup = new Dictionary<string, GameObject>(entries.Count);
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.seedName) && e.maturePrefab != null)
                lookup[e.seedName] = e.maturePrefab;
    }

    public GameObject GetMaturePrefab(string seedName)
    {
        if (lookup == null) RebuildLookup();
        return lookup.TryGetValue(seedName, out var prefab) ? prefab : null;
    }
}
