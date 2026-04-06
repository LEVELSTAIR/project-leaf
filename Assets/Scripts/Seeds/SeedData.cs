// SeedData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Farming/Seed Data")]
public class SeedData : ScriptableObject
{
    public string seedName;
    public Sprite seedIcon;
    public float growthTime = 60f; // in seconds
    public int harvestYield = 1;
    public string harvestItemName;
    public ItemType harvestItemType;
    public GameObject seedlingPrefab;
    public GameObject maturePlantPrefab;

}