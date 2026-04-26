using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Farming/Seed Data")]
public class SeedData : ScriptableObject
{
    public string seedName;
    public Sprite seedIcon;
    public float growthTime = 60f;          // seconds to mature when constantly watered
    public float waterRequired = 30f;       // total water units needed to fully grow
    public int harvestYield = 1;
    public string harvestItemName;
    public ItemType harvestItemType;
    public GameObject seedlingPrefab;
    public GameObject maturePlantPrefab;
}
