using UnityEngine;
using System.Collections.Generic;

public class AnimalObjectGenerator : MonoBehaviour
{
    [Header("Target Terrain")]
    public Terrain terrain;                     // Leave blank to use Terrain.activeTerrain

    [Header("Animal Prefabs")]
    public GameObject[] animalPrefabs;         // Prefabs to randomly pick from (e.g., deer, rabbit)

    [Header("Generation Settings")]
    public int totalAnimals = 20;              // Number of animals to place
    public bool randomRotation = true;
    public float heightOffset = 0f;            // Lift a little to avoid clipping

    [Header("Debug")]
    [SerializeField] private bool showBounds = true;

    /// <summary>
    /// Generates animal GameObjects as children. Use ContextMenu in Editor or call from Start.
    /// </summary>
    [ContextMenu("Generate Animals")]
    public void GenerateAnimals()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("No Terrain found! Assign a terrain or mark one as active.");
            return;
        }

        if (animalPrefabs == null || animalPrefabs.Length == 0)
        {
            Debug.LogError("Assign at least one animal prefab.");
            return;
        }

        // 1. Clear any previously generated animals (children of this object)
        ClearChildren();

        // 2. Get the spawn volume from the collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("This script requires a Collider on the same GameObject!");
            return;
        }
        Bounds bounds = col.bounds;

        // 3. Terrain reference
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        // 4. Spawn animals
        for (int i = 0; i < totalAnimals; i++)
        {
            // Random position inside the collider's world-space XZ
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Discard if outside terrain rectangle
            if (randomPos.x < terrainPos.x || randomPos.x > terrainPos.x + terrainSize.x ||
                randomPos.z < terrainPos.z || randomPos.z > terrainPos.z + terrainSize.z)
                continue;

            // Sample terrain height
            float terrainHeight = terrain.SampleHeight(randomPos);
            randomPos.y = terrainHeight + heightOffset;

            // Pick a random prefab
            GameObject prefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];
            if (prefab == null) continue;

            // Instantiate WITHOUT immediate parent and then SetParent with worldPositionStays = true
            // to preserve the prefab's original scale regardless of the volume's scale.
            GameObject newAnimal = Instantiate(prefab, randomPos, Quaternion.identity);
            newAnimal.transform.SetParent(transform, true);

            // Random yaw rotation
            if (randomRotation)
                newAnimal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Nice hierarchy name
            newAnimal.name = prefab.name + "_" + i;
        }
    }

    /// <summary>
    /// Destroys all child objects (the previously generated animals).
    /// Works in Edit Mode and Play Mode.
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

    // Optional: draw bounds in Scene view
    private void OnDrawGizmosSelected()
    {
        if (!showBounds) return;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
