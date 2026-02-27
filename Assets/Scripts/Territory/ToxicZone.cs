using UnityEngine;

public class ToxicZone : MonoBehaviour
{
    [Header("Settings")]
    public float damagePerSecond = 5f;
    public float oxygenCostPerSecond = 10f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager.Instance?.SetInBadLands(true);
            NotificationManager.Instance?.ShowNotification("WARNING: In BadLands! Oxygen Dropping.", 0.1f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager.Instance?.SetInBadLands(false);
        }
    }
}
