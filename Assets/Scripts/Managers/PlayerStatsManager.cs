using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    [Header("Oxygen Settings")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public float oxygenDepletionRate = 10f; // Percent per second in BadLands
    public float oxygenRegenRate = 20f;      // Percent per second in SafeZones

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float healthDepletionRate = 10f; // Percent per second when oxygen is 0

    private bool isInBadLands = false;
    private bool isInSafeZone = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentOxygen = maxOxygen;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        HandleOxygen();
        HandleHealth();
        UpdateHUD();
    }

    private void HandleOxygen()
    {
        if (isInBadLands && !isInSafeZone)
        {
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
        }
        else if (isInSafeZone)
        {
            currentOxygen += oxygenRegenRate * Time.deltaTime;
        }

        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
    }

    private void HandleHealth()
    {
        if (currentOxygen <= 0)
        {
            // Requirement: health should deplete in 10 seconds if oxygen is 0
            // That means 10% per second
            currentHealth -= (maxHealth / 10f) * Time.deltaTime;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }

    private void UpdateHUD()
    {
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateOxygen(currentOxygen / maxOxygen);
            HUDManager.Instance.UpdateHealth(currentHealth / maxHealth);
        }
    }

    public void SetInBadLands(bool value)
    {
        isInBadLands = value;
    }

    public void SetInSafeZone(bool value)
    {
        isInSafeZone = value;
    }

    private void OnDeath()
    {
        // Handle player death
        Debug.Log("Player has died from lack of oxygen!");
        NotificationManager.Instance?.ShowNotification("YOU DIED: Lack of Oxygen");
        // Reload or reset?
    }
}
