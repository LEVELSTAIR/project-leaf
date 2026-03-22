using UnityEngine;

[CreateAssetMenu(fileName = "New Seed", menuName = "Inventory/Seed")]
public class SeedData : ScriptableObject
{
    public string seedName;
    public string seedCount;
    public Sprite icon;
    public int basePrice;
    public float growthTime;
    public int harvestYield;
    public GameObject plantPrefab;
}