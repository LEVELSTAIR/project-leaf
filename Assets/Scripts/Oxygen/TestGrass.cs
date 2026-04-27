using UnityEngine;

public class TestGrass : MonoBehaviour
{
    public int detailIndex = 0;
    public int patchSize = 30;  // will paint a 30x30 patch

    private void Start()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No active terrain found!");
            return;
        }

        TerrainData tData = terrain.terrainData;

        // Safety check
        if (detailIndex >= tData.detailPrototypes.Length || detailIndex < 0)
        {
            Debug.LogError($"Invalid detail index {detailIndex}. Terrain has {tData.detailPrototypes.Length} prototypes.");
            return;
        }

        // Paint at the center of the terrain
        int cx = tData.detailWidth / 2;
        int cy = tData.detailHeight / 2;
        int half = patchSize / 2;

        int[,] details = new int[patchSize, patchSize];
        for (int y = 0; y < patchSize; y++)
            for (int x = 0; x < patchSize; x++)
                details[y, x] = 10;   // medium grass density

        tData.SetDetailLayer(cx - half, cy - half, detailIndex, details);
    }
}