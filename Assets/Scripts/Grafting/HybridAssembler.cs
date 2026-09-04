using Arborvale.Shared;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiates a hybrid plant visual by assembling PlantPartDef prefabs onto
/// a HybridPlantRoot hierarchy. The root must have named child transforms:
///   Socket_Crown  (child of trunk, where foliage attaches)
///   Socket_Bloom  (one or more children of foliage, where blooms attach)
/// </summary>
public class HybridAssembler : MonoBehaviour
{
    public static HybridAssembler Instance { get; private set; }

    [Header("Part Library")]
    public List<PlantPartDef> allParts = new List<PlantPartDef>();

    [Header("Root Prefab")]
    [Tooltip("Prefab with empty socket transforms; parts are spawned as children.")]
    public GameObject hybridRootPrefab;

    private Dictionary<string, PlantPartDef> partById;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        partById = new Dictionary<string, PlantPartDef>();
        foreach (var p in allParts)
            if (p != null && !string.IsNullOrEmpty(p.partId))
                partById[p.partId] = p;
    }

    /// <summary>
    /// Assembles a hybrid plant at the given position from a hybridId string.
    /// Returns the instantiated root, or null on failure.
    /// </summary>
    public GameObject Assemble(string hybridId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!HybridId.TryDecode(hybridId, out var trunkId, out var foliageId, out var bloomId))
        {
            Debug.LogWarning($"[HybridAssembler] Cannot decode hybridId: {hybridId}");
            return null;
        }

        if (hybridRootPrefab == null)
        {
            Debug.LogError("[HybridAssembler] hybridRootPrefab not assigned.");
            return null;
        }

        var root = Instantiate(hybridRootPrefab, position, rotation, parent);

        AttachPart(root, trunkId,   "Socket_Trunk",  out Transform crownSocket);
        AttachPart(root, foliageId, "Socket_Crown",  out Transform bloomSocket, crownSocket);
        if (bloomSocket != null)
            AttachPart(root, bloomId, "Socket_Bloom", out _, bloomSocket);

        return root;
    }

    private void AttachPart(GameObject root, string partId, string socketName, out Transform childSocket, Transform searchUnder = null)
    {
        childSocket = null;
        if (!partById.TryGetValue(partId, out var def) || def.prefab == null)
        {
            Debug.LogWarning($"[HybridAssembler] Part '{partId}' not found in library.");
            return;
        }

        Transform socket = (searchUnder ?? root.transform).Find(socketName);
        if (socket == null)
            socket = searchUnder ?? root.transform;

        var instance = Instantiate(def.prefab, socket.position, socket.rotation, socket);
        childSocket = instance.transform.Find(socketName) ?? instance.transform.Find("Socket_Crown") ?? instance.transform;
    }
}
