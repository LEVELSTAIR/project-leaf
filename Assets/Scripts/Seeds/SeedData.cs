using UnityEngine;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Farming/Seed Data")]
public class SeedData : ScriptableObject
{
    public string seedName;
    public Sprite seedIcon;
    public float growthTime = 60f;          // seconds to mature when constantly watered
    public float waterRequired = 30f;       // total water units needed to fully grow
    public int harvestYield = 1;
    public int maxHarvests = 3;
    public float oxygenAreaRadius = 10f;
    public string harvestItemName;
    public ItemType harvestItemType;
    public GameObject seedlingPrefab;
    public GameObject maturePlantPrefab;

    [Header("Flower Seed Settings")]
    public bool producesFlowerSeeds = false;
    public List<FlowerSeedYield> flowerSeedYields = new List<FlowerSeedYield>();
}

[System.Serializable]
public class FlowerSeedYield
{
    public FlowerSeedData flowerSeedData;  // Reference to FlowerSeedData asset
    public int minYield = 5;
    public int maxYield = 10;
}
