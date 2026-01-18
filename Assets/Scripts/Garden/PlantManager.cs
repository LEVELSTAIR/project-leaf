using UnityEngine;
using System.Collections.Generic;

namespace ProjectLeaf.Garden
{
    public class PlantManager : MonoBehaviour
    {
        public static PlantManager Instance { get; private set; }

        [Header("Settings")]
        public float tickIntervalInGameHours = 0.5f; // Update plants every 30 in-game minutes

        private List<PlantController> activePlants = new List<PlantController>();
        private float lastUpdateTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // We'll use the DayNightManager's time scale to determine how many game hours have passed
            if (DayNightManager.Instance == null) return;

            // This calculates how many game hours passed since the last frame
            // timeScale in DayNightManager is (24 / (dayLengthMins * 60))
            // CurrentTime in DayNightManager is advanced as: currentTime += Time.deltaTime * timeScale;
            // So we can track the delta here too.
            
            // For simplicity, we'll just check if currentTime has advanced significantly
            // or better yet, inject a Tick into the plants.
        }

        public void RegisterPlant(PlantController plant)
        {
            if (!activePlants.Contains(plant))
                activePlants.Add(plant);
        }

        public void UnregisterPlant(PlantController plant)
        {
            if (activePlants.Contains(plant))
                activePlants.Remove(plant);
        }

        // Called from DayNightManager or a dedicated timer
        public void AdvanceGrowth(float gameHours)
        {
            foreach (var plant in activePlants)
            {
                if (plant != null)
                {
                    plant.UpdateGrowth(gameHours);
                }
            }
        }
    }
}
