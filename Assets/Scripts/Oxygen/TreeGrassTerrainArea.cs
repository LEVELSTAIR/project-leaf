using UnityEngine;

public class TreeGrassTerrainArea : MonoBehaviour
{
    [SerializeField] private int detailPrototypeIndex = 0;
    [SerializeField] private int density = 300;
    [SerializeField] private float noiseDensity = 0.1f;
    [SerializeField] private int minHeight = 80;
    [SerializeField] private int maxHeight = 180;
    [SerializeField] private float fadeOuterRadius = 0.7f;
    [SerializeField] private float radiusMultiplier = 1.0f;
    [SerializeField] private bool clearGrassInEditor = true;

    private TreeOxygenArea oxygenArea;
    private Terrain terrain;
    private static bool hasInitialClear = false;

    public int DetailPrototypeIndex
    {
        get => detailPrototypeIndex;
        set => detailPrototypeIndex = value;
    }

    private void Start()
    {
        oxygenArea = GetComponent<TreeOxygenArea>();
        if (oxygenArea == null)
        {
            Debug.LogError("[TreeGrass] No TreeOxygenArea found on this GameObject!");
            return;
        }

        terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[TreeGrass] No terrain found!");
            return;
        }

#if UNITY_EDITOR
        if (clearGrassInEditor && !hasInitialClear)
        {
            ClearAllGrassOnTerrain();
            hasInitialClear = true;
            Debug.Log("[TreeGrass] Initial terrain clear completed.");
        }
#endif
        SpawnGrassDetail();
    }

    public void SetGrassType(int newIndex)
    {
        detailPrototypeIndex = newIndex;
    }

    private void ClearAllGrassOnTerrain()
    {
        if (terrain == null || terrain.terrainData == null) return;
        TerrainData terrainData = terrain.terrainData;
        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length) return;

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;
        int[,] emptyDetails = new int[detailHeight, detailWidth];
        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, emptyDetails);
        Debug.Log($"[TreeGrass] Cleared entire grass detail layer {detailPrototypeIndex}");
    }

    public void ClearGrassAroundTree(float radius)
    {
        if (terrain == null || terrain.terrainData == null) return;
        TerrainData terrainData = terrain.terrainData;
        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length) return;

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

        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                if (px < 0 || px >= detailWidth || py < 0 || py >= detailHeight) continue;

                float dist = Mathf.Sqrt(x * x + y * y) / (float)pixelRadius;
                if (dist <= 1f)
                {
                    details[py, px] = 0;
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, detailPrototypeIndex, details);
        Debug.Log($"[TreeGrass] Cleared grass area around '{name}' (radius {radius}m)");
    }

    public void ClearGrass()
    {
        if (oxygenArea == null)
            oxygenArea = GetComponent<TreeOxygenArea>();
        if (oxygenArea == null)
        {
            Debug.LogWarning($"[TreeGrass] No TreeOxygenArea on {name}, cannot clear grass.");
            return;
        }
        float radius = oxygenArea.GetOxygenRadius();
        ClearGrassAroundTree(radius);
    }

    private void SpawnGrassDetail()
    {
        float radius = oxygenArea.GetOxygenRadius();
        if (radius <= 0f) return;

        TerrainData terrainData = terrain.terrainData;
        if (detailPrototypeIndex >= terrainData.detailPrototypes.Length)
        {
            Debug.LogWarning($"Detail prototype index {detailPrototypeIndex} out of range.");
            return;
        }

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
        int pixelRadius = Mathf.RoundToInt(radius * avgPixelsPerMeter * radiusMultiplier);

        if (pixelRadius < 5) pixelRadius = 5;

        int[,] details = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, detailPrototypeIndex);
        int grassSpawned = 0;
        int totalAttempts = 0;

        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                if (px < 0 || px >= detailWidth || py < 0 || py >= detailHeight) continue;

                totalAttempts++;
                float dist = Mathf.Sqrt(x * x + y * y) / (float)pixelRadius;

                if (dist <= 1f)
                {
                    float edgeFade = 1f;
                    if (dist > fadeOuterRadius)
                        edgeFade = Mathf.Lerp(1f, 0f, (dist - fadeOuterRadius) / (1f - fadeOuterRadius));

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
        Debug.Log($"[TreeGrass] Spawned {grassSpawned} grass patches around '{name}'");
    }
}