using UnityEngine;
using System.Collections.Generic;

public class TreeObjectGenerator : MonoBehaviour
{
    [Header("Target Terrain")]
    public Terrain terrain;                     // Leave blank to use Terrain.activeTerrain

    [Header("Tree Prefabs")]
    public GameObject[] treePrefabs;            // Prefabs to randomly pick from

    [Header("Generation Settings")]
    public int totalTrees = 50;                 // Exact number of trees to place
    public bool randomRotation = true;
    public float heightOffset = 0f;             // Lift trees a bit to avoid clipping

    [Header("Debug")]
    [SerializeField] private bool showBounds = true;

    /// <summary>
    /// Generates trees as child GameObjects, preserving original prefab size.
    /// Call from Start, a button, or ContextMenu.
    /// </summary>
    [ContextMenu("Generate Trees")]
    public void GenerateTrees()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("No Terrain found! Assign a terrain or mark one as active.");
            return;
        }

        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("Assign at least one tree prefab.");
            return;
        }

        // 1. Clear previous generation
        ClearChildren();

        // 2. Get the collider volume (the spawn area)
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("This script requires a Collider on the same GameObject!");
            return;
        }
        Bounds bounds = col.bounds;

        // 3. Terrain bounds and height sampling reference
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        // 4. Spawn trees
        for (int i = 0; i < totalTrees; i++)
        {
            // Random horizontal position inside the volume's world-space XZ
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Discard if outside the terrain rectangle
            if (randomPos.x < terrainPos.x || randomPos.x > terrainPos.x + terrainSize.x ||
                randomPos.z < terrainPos.z || randomPos.z > terrainPos.z + terrainSize.z)
                continue;

            // Sample terrain height and apply offset
            float terrainHeight = terrain.SampleHeight(randomPos);
            randomPos.y = terrainHeight + heightOffset;

            // Pick a random prefab
            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            if (prefab == null) continue;

            // *** THE FIX: Instantiate WITHOUT immediate parenting, 
            // then use SetParent with worldPositionStays=true to keep EXACT prefab size ***
            GameObject newTree = Instantiate(prefab, randomPos, Quaternion.identity);
            newTree.transform.SetParent(transform, true);   // <-- preserves world scale

            // Apply random yaw rotation (world space)
            if (randomRotation)
                newTree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Nice naming
            newTree.name = prefab.name + "_" + i;
        }
    }

    /// <summary>
    /// Removes all previously generated child trees (Edit Mode and Play Mode safe).
    /// </summary>
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    // Optional gizmo to visualise the volume
    private void OnDrawGizmosSelected()
    {
        if (!showBounds) return;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
