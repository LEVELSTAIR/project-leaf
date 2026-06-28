using UnityEngine;
using System.Collections;

/// <summary>
/// Listens to PlayerOxygen's depleted state and deals damage to the player's health.
/// </summary>
public class OxygenDamageHandler : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damagePerSecond = 5;        // HP lost per second at 0 oxygen
    [SerializeField] private float damageTickInterval = 0.5f; // how often to apply damage
    [SerializeField] private float startupDelay = 0f;        // optional grace period after depletion

    private PlayerOxygen oxygen;
    private PlayerHealthManager health;
    private Coroutine damageCoroutine;

    private void Start()
    {
        // Get references on the same GameObject (the player)
        oxygen = GetComponent<PlayerOxygen>();
        health = GetComponent<PlayerHealthManager>();

        if (oxygen == null)
            Debug.LogError("OxygenDamageHandler: No PlayerOxygen found on this GameObject.");
        if (health == null)
            Debug.LogError("OxygenDamageHandler: No PlayerHealthManager found on this GameObject.");

        // Subscribe to the depleted state change event
        if (oxygen != null)
            oxygen.onDepletedStateChanged.AddListener(OnDepletedStateChanged);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (oxygen != null)
            oxygen.onDepletedStateChanged.RemoveListener(OnDepletedStateChanged);
    }

    private void OnDepletedStateChanged(bool isDepleted)
    {
        if (isDepleted)
        {
            // Start damaging if we aren't already and player is alive
            if (damageCoroutine == null && health != null && health.IsAlive())
                damageCoroutine = StartCoroutine(DamageOverTime());
        }
        else
        {
            // Stop damaging
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageOverTime()
    {
        // Optional grace period before first damage
        if (startupDelay > 0f)
            yield return new WaitForSeconds(startupDelay);

        while (health != null && health.IsAlive())
        {
            // Calculate damage per tick (ceil to ensure at least 1 HP per tick)
            int damagePerTick = Mathf.CeilToInt(damagePerSecond * damageTickInterval);
            health.TakeDamage(damagePerTick);

            // Wait for next tick
            yield return new WaitForSeconds(damageTickInterval);
        }

        // If player dies, the coroutine stops naturally
        damageCoroutine = null;
    }
}