using UnityEngine;

[CreateAssetMenu(fileName = "New FlowerSeed", menuName = "Farming/Flower Seed Data")]
public class FlowerSeedData : ScriptableObject
{
    public string seedName;
    public Sprite seedIcon;
    public GameObject flowerPrefab;
}
