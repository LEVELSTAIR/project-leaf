using UnityEngine;

namespace ProjectLeaf.Data
{
    [CreateAssetMenu(fileName = "NewSeedData", menuName = "Project Leaf/Seed Data")]
    public class SeedData : ScriptableObject
    {
        public string seedName;
        public Sprite icon;
        public PlantData plantToGrow;
        public bool isNightSeed;
        [Range(0, 1)]
        public float antiPlantChance = 0.5f; // 50% for night seeds as per design
    }
}
