using UnityEngine;

/// <summary>
/// Attach to a crafted object to clear grass from the terrain within a radius around it.
/// Works with the same terrain detail layer used by TreeGrassTerrainArea.
/// </summary>
public class GrassClearer : MonoBehaviour
{
    [Tooltip("Index of the terrain detail prototype (grass layer) to clear.")]
    [SerializeField] private int detailPrototypeIndex = 0;

    [Tooltip("Radius (in world units) around the object within which grass will be cleared.")]
    [SerializeField] private float clearRadius = 2f;

    [Tooltip("If true, clears grass in Start(). Set false if you want to call ClearGrass() manually.")]
    [SerializeField] private bool clearOnStart = true;

    private Terrain terrain;
    private TerrainData terrainData;

    private void Start()
    {
        if (clearOnStart)
        {
            ClearGrass();
        }
    }

    /// <summary>
    /// Clears grass from the terrain inside a circle around the object's position.
    /// Call this manually if clearOnStart is false.
    /// </summary>
    public void ClearGrass()
    {
        // Find the active terrain
        terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("GrassClearer: No active terrain found!");
            return;
        }

        terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("GrassClearer: TerrainData is missing!");
            return;
        }

        // Validate detail prototype index
        if (detailPrototypeIndex < 0 || detailPrototypeIndex >= terrainData.detailPrototypes.Length)
        {
            Debug.LogError($"GrassClearer: detailPrototypeIndex {detailPrototypeIndex} is out of range (0-{terrainData.detailPrototypes.Length - 1})");
            return;
        }

        // Convert world position to terrain local position
        Vector3 worldPos = transform.position;
        Vector3 terrainLocalPos = worldPos - terrain.transform.position;

        // Get detail map resolution
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // Normalized coordinates (0..1)
        float normX = terrainLocalPos.x / terrainData.size.x;
        float normZ = terrainLocalPos.z / terrainData.size.z;

        // Clamp to terrain bounds (avoid negative/out-of-range indices)
        normX = Mathf.Clamp01(normX);
        normZ = Mathf.Clamp01(normZ);

        // Detail map pixel coordinates of the object's center
        int centerX = Mathf.RoundToInt(normX * detailWidth);
        int centerY = Mathf.RoundToInt(normZ * detailHeight);

        // Compute radius in detail map pixels
        float pixelsPerMeterX = detailWidth / terrainData.size.x;
        float pixelsPerMeterZ = detailHeight / terrainData.size.z;
        float avgPixelsPerMeter = (pixelsPerMeterX + pixelsPerMeterZ) * 0.5f;
        int radiusInPixels = Mathf.RoundToInt(clearRadius * avgPixelsPerMeter);
        if (radiusInPixels < 1) radiusInPixels = 1;

        // Get current detail layer (grass densities)
        int[,] details = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, detailPrototypeIndex);

        int clearedCount = 0;

        // Iterate over the bounding square that covers the circle
        for (int y = -radiusInPixels; y <= radiusInPixels; y++)
        {
            for (int x = -radiusInPixels; x <= radiusInPixels; x++)
            {
                int px = centerX + x;
                int py = centerY + y;

                // Skip pixels outside terrain detail map
                if (px < 0 || px >= detailWidth || py < 0 || py >= detailHeight)
                    continue;

                // Check if inside the circle
                float dist = Mathf.Sqrt(x * x + y * y);
                if (dist <= radiusInPixels)
                {
                    // Clear grass (set density to 0)
                    if (details[py, px] > 0)
                    {
                        details[py, px] = 0;
                        clearedCount++;
                    }
                }
            }
        }

        // Apply modified detail layer back to terrain
        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, details);

        Debug.Log($"[GrassClearer] Cleared {clearedCount} grass patches on layer {detailPrototypeIndex} " +
                  $"around '{gameObject.name}' (radius {clearRadius}m)");
    }

    // Optional: draw the clear radius in the Scene view for visual feedback
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, clearRadius);
    }
}