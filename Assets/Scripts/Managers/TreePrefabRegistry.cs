using UnityEngine;
using System.Collections.Generic;

public class TreePrefabRegistry : MonoBehaviour
{
    public static TreePrefabRegistry Instance { get; private set; }

    [System.Serializable]
    public class PrefabEntry
    {
        public string key;
        public GameObject prefab;
    }

    [SerializeField] private List<PrefabEntry> prefabs = new List<PrefabEntry>();

    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var entry in prefabs)
        {
            if (!prefabDict.ContainsKey(entry.key))
                prefabDict.Add(entry.key, entry.prefab);
            else
                Debug.LogWarning($"Duplicate prefab key: {entry.key}");
        }
    }

    public GameObject GetPrefab(string key)
    {
        prefabDict.TryGetValue(key, out GameObject prefab);
        return prefab;
    }
}