using UnityEngine;
using System.Collections;

public class PlayerHealthManager : MonoBehaviour
{
    // ---------- Gender selection ----------
    public enum Gender { Male, Female }

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Auto Healing")]
    [SerializeField] private float autoHealDelay = 5f;
    [SerializeField] private float healAmountPerSecond = 10f;
    [SerializeField] private float healTickInterval = 0.5f;

    [Header("Invincibility Frames (optional)")]
    [SerializeField] private float invincibilityDuration = 1f;
    private float invincibilityTimer = 0f;

    [Header("Hurt Sounds")]
    [SerializeField] private Gender playerGender = Gender.Male;
    [SerializeField] private AudioClip maleHurtSound;
    [SerializeField] private AudioClip femaleHurtSound;

    // Cached references
    private HUDManager hud;
    private Coroutine autoHealCoroutine;

    // Events
    public System.Action<int, int> OnHealthChanged;
    public System.Action OnPlayerDied;

    private void Start()
    {
        currentHealth = maxHealth;

        if (HUDManager.Instance != null)
            hud = HUDManager.Instance;
        else
            Debug.LogWarning("PlayerHealthManager: HUDManager.Instance not found.");

        UpdateHUD();
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    // ref for save system
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    /// <summary>
    /// Apply saved health data.
    /// </summary>
    public void LoadFromSave(SaveData data)
    {
        maxHealth = data.maxHealth;
        currentHealth = data.currentHealth;
        if (currentHealth < 0) currentHealth = 0;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }


    /// <summary>
    /// Called when the player takes damage.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (invincibilityTimer > 0f) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // Invincibility frames
        if (invincibilityDuration > 0f)
            invincibilityTimer = invincibilityDuration;

        // ---- Play hurt sound ----
        PlayHurtSound();

        // Flash HUD
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowDamageFlash();

        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Restart auto-heal delay
        if (autoHealCoroutine != null)
            StopCoroutine(autoHealCoroutine);
        autoHealCoroutine = StartCoroutine(AutoHealRoutine());

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Heal the player by a specific amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void FullHeal()
    {
        currentHealth = maxHealth;
        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public bool IsAlive() => currentHealth > 0;

    /// <summary>
    /// Change the player's gender at runtime.
    /// </summary>
    public void SetGender(Gender newGender) => playerGender = newGender;

    private void UpdateHUD()
    {
        if (hud != null)
            hud.UpdateHealth(GetHealthPercentage());
    }

    private IEnumerator AutoHealRoutine()
    {
        yield return new WaitForSeconds(autoHealDelay);

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

        if (autoHealCoroutine != null)
            StopCoroutine(autoHealCoroutine);

        // Optionally trigger game over
        // GameManager.Instance.GameOver();
    }

    public void Respawn()
    {
        currentHealth = maxHealth;
        invincibilityTimer = 0f;
        UpdateHUD();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ---------- Sound helpers ----------
    private void PlayHurtSound()
    {
        AudioClip clip = (playerGender == Gender.Male) ? maleHurtSound : femaleHurtSound;
        if (clip == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXOneShot(clip);
        else
            Debug.LogWarning("SoundManager.Instance not found – hurt sound not played.");
    }
}