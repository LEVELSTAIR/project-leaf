using System.Collections.Generic;
using UnityEngine;

public class GameObjectsToTerrainTrees : MonoBehaviour
{
    public Terrain terrain;

    // Parent containing converted tree GameObjects
    public Transform treesParent;

    [ContextMenu("Convert GameObjects To Terrain Trees")]
    void ConvertToTerrainTrees()
    {
        if (terrain == null || treesParent == null)
        {
            Debug.LogError("Assign Terrain and Trees Parent!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        List<TreeInstance> treeInstances =
            new List<TreeInstance>(terrainData.treeInstances);

        foreach (Transform treeObj in treesParent)
        {
            GameObject prefab = FindMatchingPrototype(
                treeObj.gameObject,
                terrainData
            );

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"No matching terrain tree prototype found for {treeObj.name}"
                );
                continue;
            }

            int prototypeIndex = GetPrototypeIndex(prefab, terrainData);

            Vector3 terrainPos =
                treeObj.position - terrain.transform.position;

            Vector3 normalizedPos = new Vector3(
                terrainPos.x / terrainData.size.x,
                terrainPos.y / terrainData.size.y,
                terrainPos.z / terrainData.size.z
            );

            TreeInstance tree = new TreeInstance();

            tree.position = normalizedPos;
            tree.prototypeIndex = prototypeIndex;

            tree.widthScale = treeObj.localScale.x;
            tree.heightScale = treeObj.localScale.y;

            tree.rotation =
                treeObj.eulerAngles.y * Mathf.Deg2Rad;

            tree.color = Color.white;
            tree.lightmapColor = Color.white;

            treeInstances.Add(tree);
        }

        terrainData.treeInstances = treeInstances.ToArray();

        Debug.Log("Converted GameObjects back to Terrain Trees!");
    }

    GameObject FindMatchingPrototype(
        GameObject obj,
        TerrainData terrainData
    )
    {
        foreach (TreePrototype prototype in terrainData.treePrototypes)
        {
            if (prototype.prefab.name == obj.name.Replace("(Clone)", "").Trim())
            {
                return prototype.prefab;
            }
        }

        return null;
    }

    int GetPrototypeIndex(GameObject prefab, TerrainData terrainData)
    {
        for (int i = 0; i < terrainData.treePrototypes.Length; i++)
        {
            if (terrainData.treePrototypes[i].prefab == prefab)
            {
                return i;
            }
        }

        return -1;
    }
}