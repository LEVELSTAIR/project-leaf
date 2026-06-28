using UnityEngine;
using System.Collections;

/// <summary>
/// Applies damage to the player while they remain inside the trigger collider.
/// Works with PlayerHealthManager and supports continuous damage, on‑enter damage,
/// sounds, and visual effects.
/// </summary>
public class DangerZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damagePerSecond = 10;        // HP lost per second while inside
    [SerializeField] private float tickInterval = 0.5f;       // how often to apply damage
    [SerializeField] private int damageOnEnter = 0;           // immediate damage when entering (optional)
    [SerializeField] private bool damageOnlyIfAlive = true;   // stop damaging if player dies

    [Header("Delay & Cooldown")]
    [SerializeField] private float initialDelay = 0f;         // wait before first damage tick (grace period)
    [SerializeField] private float cooldownAfterExit = 0f;    // extra time before re‑entering triggers damage again

    [Header("Sound & Effects")]
    [SerializeField] private AudioClip enterSound;            // played when entering the zone
    [SerializeField] private AudioClip exitSound;             // played when leaving
    [SerializeField] private AudioClip damageTickSound;       // played each damage tick (optional)
    [SerializeField] private GameObject enterEffect;          // instantiated on enter
    [SerializeField] private GameObject exitEffect;           // instantiated on exit

    [Header("Layer Filter (optional)")]
    [SerializeField] private LayerMask playerLayerMask = ~0;  // default: all layers; set to "Player" layer for safety

    // Private state
    private bool playerInside = false;
    private Coroutine damageCoroutine;
    private float lastExitTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player (by tag or layer)
        if (!IsPlayer(other.gameObject)) return;

        // Cooldown check (if we left recently, prevent re‑entry damage)
        if (cooldownAfterExit > 0f && Time.time - lastExitTime < cooldownAfterExit)
            return;

        playerInside = true;

        // Play enter sound
        PlaySound(enterSound);

        // Instantiate enter effect
        if (enterEffect != null)
            Instantiate(enterEffect, transform.position, Quaternion.identity);

        // Apply optional one‑time damage on enter
        if (damageOnEnter > 0)
        {
            ApplyDamage(damageOnEnter);
        }

        // Start the damage over time coroutine
        if (damageCoroutine == null)
            damageCoroutine = StartCoroutine(DamageOverTime());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other.gameObject)) return;

        playerInside = false;
        lastExitTime = Time.time;

        // Play exit sound
        PlaySound(exitSound);

        // Instantiate exit effect
        if (exitEffect != null)
            Instantiate(exitEffect, transform.position, Quaternion.identity);

        // Stop the damage coroutine
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private bool IsPlayer(GameObject obj)
    {
        // Option 1: check tag
        if (obj.CompareTag("Player")) return true;

        // Option 2: check layer (if you set a specific layer for the player)
        if (((1 << obj.layer) & playerLayerMask) != 0) return true;

        // Option 3: check if it has PlayerHealthManager (more robust)
        if (obj.GetComponent<PlayerHealthManager>() != null) return true;

        return false;
    }

    private IEnumerator DamageOverTime()
    {
        // Optional initial delay
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        // Keep damaging while the player is inside and alive
        while (playerInside)
        {
            // Check if player is still valid
            if (damageOnlyIfAlive)
            {
                PlayerHealthManager health = FindObjectOfType<PlayerHealthManager>();
                if (health != null && !health.IsAlive())
                {
                    // Player is dead – stop damaging
                    break;
                }
            }

            // Apply damage per tick (ceil to ensure at least 1 HP per tick)
            int damagePerTick = Mathf.CeilToInt(damagePerSecond * tickInterval);
            ApplyDamage(damagePerTick);

            // Play tick sound (optional)
            if (damageTickSound != null)
                PlaySound(damageTickSound);

            // Wait for next tick
            yield return new WaitForSeconds(tickInterval);
        }

        damageCoroutine = null;
    }

    private void ApplyDamage(int amount)
    {
        // Find the player's health manager
        PlayerHealthManager health = FindObjectOfType<PlayerHealthManager>();
        if (health != null && health.IsAlive())
        {
            health.TakeDamage(amount);
            Debug.Log($"{gameObject.name} dealt {amount} damage to player.");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXOneShot(clip);
        else
            Debug.LogWarning("SoundManager.Instance not found – sound not played.");
    }

    // Optional: show gizmos for editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Draw the trigger bounds (if it's a box collider)
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}