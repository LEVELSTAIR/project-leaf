using UnityEngine;

public class TerrainTreeToGameObjects : MonoBehaviour
{
    public Terrain terrain;

    [ContextMenu("Convert Trees To GameObjects")]
    void ConvertTrees()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        TreeInstance[] trees = terrainData.treeInstances;

        foreach (TreeInstance tree in trees)
        {
            // Get prefab from tree prototype
            GameObject treePrefab =
                terrainData.treePrototypes[tree.prototypeIndex].prefab;

            if (treePrefab == null)
                continue;

            // Convert normalized terrain position to world position
            Vector3 worldPos = Vector3.Scale(tree.position, terrainData.size)
                               + terrain.transform.position;

            // Instantiate tree as normal GameObject
            GameObject newTree = Instantiate(
                treePrefab,
                worldPos,
                Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0)
            );

            // Apply scale
            newTree.transform.localScale = new Vector3(
                tree.widthScale,
                tree.heightScale,
                tree.widthScale
            );

            newTree.name = treePrefab.name;
        }

        Debug.Log("Tree conversion completed!");
    }
}