using UnityEngine;
using ProjectLeaf.Interfaces;

namespace ProjectLeaf.Garden
{
    public class PotController : MonoBehaviour, IInteractable
    {
        [Header("Pot Settings")]
        public Transform plantSpawnPoint;
        public GameObject currentPlant;

        public string InteractionPrompt
        {
            get
            {
                if (currentPlant == null) return "Plant Seed";
                // Check if plant needs water or is harvestable?
                return "Interact with Plant";
            }
        }

        public void Interact()
        {
            if (currentPlant == null)
            {
                // Open Seed Selection UI?
                // For now, simulate planting a default seed if player has one
                NotificationManager.Instance?.ShowNotification("Select a seed to plant (UI Todo)");
                // Logic: Check inventory -> SeedManager -> Instantiate Plant
            }
            else
            {
                // Pass interaction to the plant itself
                var plantCtrl = currentPlant.GetComponent<PlantController>();
                if (plantCtrl != null)
                {
                    // For example: Water it? 
                    plantCtrl.WaterPlant();
                }
            }
        }

        public void PlantSeed(GameObject plantPrefab)
        {
            if (currentPlant != null) return;

            currentPlant = Instantiate(plantPrefab, plantSpawnPoint.position, Quaternion.identity);
            currentPlant.transform.SetParent(plantSpawnPoint);
            NotificationManager.Instance?.ShowNotification("Seed planted.");
        }
        public void Highlight(bool active)
        {
            var highlighter = GetComponent<Highlighter>();
            if (highlighter != null) highlighter.SetHighlight(active);
        }
    }
}
