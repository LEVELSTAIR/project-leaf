using UnityEngine;

namespace ProjectLeaf.Data
{
    [CreateAssetMenu(fileName = "NewPlantData", menuName = "Project Leaf/Plant Data")]
    public class PlantData : ScriptableObject
    {
        public string plantName;
        public Sprite icon;
        public GameObject[] growthStagePrefabs;
        public float[] growthStageDurations; // Time in game hours for each stage
        public float waterNeededPerStage;
        public float droughtToleranceTime; // Grace period in game hours before death if not watered
        public bool isNightPlant;
        public float seedDropChance = 0.1f;
        public SeedData producedSeed;

        [Header("Anti-Plant Settings")]
        public bool canBeAntiPlant;
        public GameObject antiPlantPrefab;
        public float baseTamingDifficulty = 1f;
    }
}
