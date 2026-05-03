using UnityEngine;

public class TreeGrassTerrainArea : MonoBehaviour
{
    [SerializeField] private int detailPrototypeIndex = 0;  // now private – still shows in Inspector
    [SerializeField] private int density = 300;  // Max density for visible grass
    [SerializeField] private float noiseDensity = 0.1f;  // Reduced noise for more consistent spawning
    [SerializeField] private int minHeight = 80;  // Changed to 0-255 range instead of 0-0.3
    [SerializeField] private int maxHeight = 180;  // More sensible range for detail density
    [SerializeField] private float fadeOuterRadius = 0.7f;  // More aggressive fade at edges
    [SerializeField] private float radiusMultiplier = 1.0f;  // Multiplier to expand grass area when detail resolution is low
    [SerializeField] private bool clearGrassInEditor = true;  // Clear painted grass when in editor mode

    private TreeOxygenArea oxygenArea;
    private Terrain terrain;
    private static bool hasInitialClear = false;  // Prevents multiple clears

    public int DetailPrototypeIndex
    {
        get => detailPrototypeIndex;
        set => detailPrototypeIndex = value;
    }

    private void Start()
    {
        Debug.Log($"[TreeGrass] Start on {name} (activeInHierarchy: {gameObject.activeInHierarchy})");
        oxygenArea = GetComponent<TreeOxygenArea>();
        if (oxygenArea == null)
            Debug.LogError("No TreeOxygenArea found on this GameObject!");
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null)
            Debug.LogError("No terrain found!");
        else
        {
            // In editor mode, clear the entire terrain ONLY ONCE before any trees spawn grass
            #if UNITY_EDITOR
            if (clearGrassInEditor && !hasInitialClear)
            {
                ClearAllGrassOnTerrain();
                hasInitialClear = true;
                Debug.Log("[TreeGrass] Initial terrain clear completed. Now spawning grass for all trees.");
            }
            #endif

            SpawnGrassDetail();
        }
    }

    /// <summary>
    /// Use this at runtime if you want to change the grass type.
    /// </summary>
    public void SetGrassType(int newIndex)
    {
        detailPrototypeIndex = newIndex;
    }

    /// <summary>
    /// Clears the entire detail layer on the terrain (called once during initialization).
    /// </summary>
    private void ClearAllGrassOnTerrain()
    {
        if (terrain == null || terrain.terrainData == null)
            return;

        TerrainData terrainData = terrain.terrainData;

        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length)
            return;

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // Create an empty detail layer (all zeros)
        int[,] emptyDetails = new int[detailHeight, detailWidth];

        // Set the detail layer to empty
        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, emptyDetails);

        Debug.Log($"[TreeGrass] Cleared entire grass detail layer {detailPrototypeIndex} on terrain '{terrain.name}'");
    }

    /// <summary>
    /// Clears grass only in a specific area around a tree.
    /// Use this if you want to remove a tree's grass without affecting others.
    /// </summary>
    public void ClearGrassAroundTree(float radius)
    {
        if (terrain == null || terrain.terrainData == null)
            return;

        TerrainData terrainData = terrain.terrainData;

        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length)
            return;

        Vector3 treeWorldPos = transform.position;
        Vector3 terrainPos = treeWorldPos - terrain.transform.position;

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        float normX = terrainPos.x / terrainData.size.x;
        float normZ = terrainPos.z / terrainData.size.z;

        int centerX = Mathf.RoundToInt(normX * detailWidth);
        int centerY = Mathf.RoundToInt(normZ * detailHeight);

        float pixelsPerMeterX = detailWidth / terrainData.size.x;
        float pixelsPerMeterZ = detailHeight / terrainData.size.z;
        float avgPixelsPerMeter = (pixelsPerMeterX + pixelsPerMeterZ) * 0.5f;
        int pixelRadius = Mathf.RoundToInt(radius * avgPixelsPerMeter);

        int[,] details = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, detailPrototypeIndex);

        // Clear only the circular area around this tree
        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                if (px < 0 || px >= detailWidth || py < 0 || py >= detailHeight)
                    continue;

                float dist = Mathf.Sqrt(x * x + y * y) / (float)pixelRadius;
                if (dist <= 1f)
                {
                    details[py, px] = 0;  // Clear this pixel
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, details);
        Debug.Log($"[TreeGrass] Cleared grass area around tree '{name}'");
    }

    private void SpawnGrassDetail()
    {
        // Get the actual oxygen area radius (uses SeedData if available, falls back to collider)
        float radius = oxygenArea.GetOxygenRadius();

        Debug.Log($"SpawnGrassDetail: tree pos {transform.position}, oxygen area radius {radius}, " +
          $"terrain: {terrain != null}, index valid: {detailPrototypeIndex < terrain.terrainData.detailPrototypes.Length}");

        TerrainData terrainData = terrain.terrainData;

        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length)
        {
            Debug.LogWarning($"Detail prototype index {detailPrototypeIndex} out of range. Max is {terrainData.detailPrototypes.Length - 1}");
            return;
        }
        Vector3 treeWorldPos = transform.position;

        Vector3 terrainPos = treeWorldPos - terrain.transform.position;

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // Calculate normalized position (0-1 range on terrain)
        float normX = terrainPos.x / terrainData.size.x;
        float normZ = terrainPos.z / terrainData.size.z;

        // Convert to detail map coordinates
        int centerX = Mathf.RoundToInt(normX * detailWidth);
        int centerY = Mathf.RoundToInt(normZ * detailHeight);

        // Calculate radius in detail map pixels
        // The detail map covers the entire terrain, so we scale radius by the detail resolution / terrain size
        float pixelsPerMeterX = detailWidth / terrainData.size.x;
        float pixelsPerMeterZ = detailHeight / terrainData.size.z;
        float avgPixelsPerMeter = (pixelsPerMeterX + pixelsPerMeterZ) * 0.5f;
        int pixelRadius = Mathf.RoundToInt(radius * avgPixelsPerMeter * radiusMultiplier);  // Apply multiplier

        Debug.Log($"=== TERRAIN DEBUG INFO ===");
        Debug.Log($"Detail map resolution: {detailWidth}x{detailHeight}");
        Debug.Log($"Terrain size: {terrainData.size.x}x{terrainData.size.z}");
        Debug.Log($"Pixels per meter X: {pixelsPerMeterX}");
        Debug.Log($"Pixels per meter Z: {pixelsPerMeterZ}");
        Debug.Log($"Avg pixels per meter: {avgPixelsPerMeter}");
        Debug.Log($"World radius: {radius}m");
        Debug.Log($"Radius multiplier: {radiusMultiplier}");
        Debug.Log($"Calculated pixel radius: {pixelRadius}");
        Debug.Log($"=== END DEBUG INFO ===");

        // Ensure minimum visible area
        if (pixelRadius < 5)
        {
            Debug.LogWarning($"Pixel radius was {pixelRadius}. Enforcing minimum of 5 pixels.");
            pixelRadius = 5;
        }

        int[,] details = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, detailPrototypeIndex);

        int grassSpawned = 0;
        int totalAttempts = 0;

        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                if (px < 0 || px >= detailWidth || py < 0 || py >= detailHeight)
                    continue;

                totalAttempts++;
                float dist = Mathf.Sqrt(x * x + y * y) / (float)pixelRadius;

                if (dist <= 1f)
                {
                    float edgeFade = 1f;
                    if (dist > fadeOuterRadius)
                        edgeFade = Mathf.Lerp(1f, 0f, (dist - fadeOuterRadius) / (1f - fadeOuterRadius));

                    // Probability: normalized density (0-255) * edge fade * noise variation
                    // With density = 255 and noiseDensity = 0.2, probability = 1.0 * edgeFade * 0.9 = 0.9 at center
                    float probability = (density / 255f) * edgeFade * (1f - noiseDensity);

                    if (Random.value < probability)
                    {
                        details[py, px] = Random.Range(minHeight, maxHeight + 1);
                        grassSpawned++;
                    }
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, details);
        Debug.Log($"Grass spawning complete. Center: ({centerX}, {centerY}), Radius: {pixelRadius} pixels");
        Debug.Log($"Total attempts: {totalAttempts}, Grass patches spawned: {grassSpawned}, Success rate: {(grassSpawned > 0 ? (100f * grassSpawned / totalAttempts).ToString("F1") : "0")}%");
    }
}