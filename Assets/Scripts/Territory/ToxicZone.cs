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
            // Check for Oxygen Bag?
            // PlayerHealthController health = other.GetComponent<PlayerHealthController>();
            // if (health != null) health.TakeDamage(damagePerSecond * Time.deltaTime);
            
            NotificationManager.Instance?.ShowNotification("WARNING: In BadLands! Oxygen Dropping.", 0.1f);
        }
    }
}
