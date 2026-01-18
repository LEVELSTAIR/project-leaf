using UnityEngine;

public class PlantCageController : MonoBehaviour
{
    [Header("Cage Settings")]
    public Transform snapPoint;
    public bool isOccupied = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (isOccupied) return;

        if (other.CompareTag("AntiPlant"))
        {
            // Lock it in
            CapturePlant(other.gameObject);
        }
    }

    private void CapturePlant(GameObject plant)
    {
        isOccupied = true;
        
        // Disable movement
        var agent = plant.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Snap to center
        plant.transform.position = snapPoint.position;
        plant.transform.rotation = snapPoint.rotation;

        // Notify Taming System
        var taming = plant.GetComponent<TamingSystem>();
        if (taming != null)
        {
            taming.SetInCage(true);
        }

        NotificationManager.Instance?.ShowNotification("Anti-Plant Captured in Cage!");
    }
}
