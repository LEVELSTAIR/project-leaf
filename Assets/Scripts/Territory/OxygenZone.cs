using UnityEngine;

public class OxygenZone : MonoBehaviour
{
    [Header("Settings")]
    public float oxygenRegenRate = 10f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // PlayerOxygenController oxygen = other.GetComponent<PlayerOxygenController>();
            // if (oxygen != null) oxygen.Refill(oxygenRegenRate * Time.deltaTime);
            
            // Visual feedback?
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
             NotificationManager.Instance?.ShowNotification("Entered Safe Zone.");
        }
    }
}
