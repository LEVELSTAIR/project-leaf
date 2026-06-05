using UnityEngine;
using System.Collections;

public class PlayerHealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Auto Healing")]
    [SerializeField] private float autoHealDelay = 5f;      // seconds after last damage before healing starts
    [SerializeField] private float healAmountPerSecond = 10f;
    [SerializeField] private float healTickInterval = 0.5f; // how often to apply healing

    [Header("Invincibility Frames (optional)")]
    [SerializeField] private float invincibilityDuration = 1f; // after being hit, cannot take damage again for this long
    private float invincibilityTimer = 0f;

    // Cached references
    private HUDManager hud;
    private Coroutine autoHealCoroutine;

    // Events (optional, for other systems to react)
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnPlayerDied;

    private void Start()
    {
        currentHealth = maxHealth;

        // Find HUDManager via singleton
        if (HUDManager.Instance != null)
            hud = HUDManager.Instance;
        else
            Debug.LogWarning("PlayerHealthManager: HUDManager.Instance not found. HUD won't update.");

        UpdateHUD();
    }

    private void Update()
    {
        // Reduce invincibility timer
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Called when the player takes damage.
    /// </summary>
    /// <param name="damage">Amount of damage</param>
    public void TakeDamage(int damage)
    {
        // If invincible, ignore damage
        if (invincibilityTimer > 0f) return;

        // Apply damage
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // Invincibility frames
        if (invincibilityDuration > 0f)
            invincibilityTimer = invincibilityDuration;

        // Flash the HUD
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowDamageFlash();

        // Update HUD
        UpdateHUD();

        // Notify listeners
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Stop current auto-heal coroutine and restart the delay
        if (autoHealCoroutine != null)
            StopCoroutine(autoHealCoroutine);
        autoHealCoroutine = StartCoroutine(AutoHealRoutine());

        // If health reaches zero, die
        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Heal the player by a specific amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // dead players cannot heal

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Fully heal the player.
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Returns current health as a percentage (0-1).
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Check if player is alive.
    /// </summary>
    public bool IsAlive() => currentHealth > 0;

    private void UpdateHUD()
    {
        if (hud != null)
            hud.UpdateHealth(GetHealthPercentage());
    }

    private IEnumerator AutoHealRoutine()
    {
        // Wait for the delay after last hit
        yield return new WaitForSeconds(autoHealDelay);

        // Then heal over time until full health
        while (currentHealth < maxHealth && currentHealth > 0)
        {
            float healThisTick = healAmountPerSecond * healTickInterval;
            currentHealth += Mathf.CeilToInt(healThisTick);
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            UpdateHUD();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            yield return new WaitForSeconds(healTickInterval);
        }

        autoHealCoroutine = null;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnPlayerDied?.Invoke();

        // Optional: disable player controls, show death UI, respawn, etc.
        // For now, we can stop auto-healing and maybe reload scene.
        if (autoHealCoroutine != null)
            StopCoroutine(autoHealCoroutine);

        // Example: trigger a game over event
        // GameManager.Instance.GameOver();
    }

    // Optional: reset health when respawning
    public void Respawn()
    {
        currentHealth = maxHealth;
        invincibilityTimer = 0f;
        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}