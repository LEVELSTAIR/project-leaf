using UnityEngine;

public class OxygenZone : MonoBehaviour
{
    [Header("Settings")]
    public float oxygenRegenRate = 10f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager.Instance?.SetInSafeZone(true);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
             NotificationManager.Instance?.ShowNotification("Entered Safe Zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager.Instance?.SetInSafeZone(false);
            NotificationManager.Instance?.ShowNotification("Leaving Safe Zone...");
        }
    }
}
