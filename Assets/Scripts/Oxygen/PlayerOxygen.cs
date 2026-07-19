using UnityEngine;
using UnityEngine.Events;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float depletionRate = 5f;
    [SerializeField] private float refillRate = 10f;

    [Header("Oxygen Sounds")]
    [SerializeField] private AudioClip intermediateSound;   // e.g. at 50%
    [SerializeField] private AudioClip lowSound;            // e.g. at 25%
    [SerializeField] private AudioClip depletedSound;       // when oxygen reaches 0

    [Header("Sound Thresholds (percentage)")]
    [SerializeField] private float intermediateThreshold = 0.5f;   // 50%
    [SerializeField] private float lowThreshold = 0.25f;           // 25%

    [Header("Events")]
    public UnityEvent onOxygenDepleted;             // fires once when hitting 0
    public UnityEvent<bool> onDepletedStateChanged; // fires when IsDepleted changes

    // Private state
    private float currentOxygen;
    private bool hasPlayedIntermediate;
    private bool hasPlayedLow;
    private bool isDepleted;   // true when oxygen == 0 and outside safe zone

    public float OxygenPercentage => currentOxygen / maxOxygen;
    public bool IsInOxygenZone { get; private set; }
    public bool IsDepleted => isDepleted;

    private void Start()
    {
        currentOxygen = maxOxygen;
        ResetSoundFlags();
        UpdateHUD();
    }

    // ref for save system
    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;

    /// <summary>
    /// Apply saved oxygen data.
    /// </summary>
    public void LoadFromSave(SaveData data)
    {
        maxOxygen = data.maxOxygen;
        currentOxygen = data.currentOxygen;
        if (currentOxygen < 0) currentOxygen = 0;
        if (currentOxygen > maxOxygen) currentOxygen = maxOxygen;
        UpdateHUD();
    }

    private void Update()
    {
        IsInOxygenZone = IsPlayerInsideAnyZone();

        if (IsInOxygenZone)
        {
            // ---------- Refill oxygen ----------
            if (refillRate > 0f)
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + refillRate * Time.deltaTime);
            else
                currentOxygen = maxOxygen;

            // Reset sound flags when above thresholds
            float percent = OxygenPercentage;
            if (percent > intermediateThreshold)
            {
                ResetSoundFlags();
            }
            else if (percent > lowThreshold && hasPlayedLow)
            {
                hasPlayedLow = false;
            }

            // If oxygen goes above 0, we are no longer depleted
            if (currentOxygen > 0f && isDepleted)
            {
                SetDepletedState(false);
            }
        }
        else
        {
            // ---------- Deplete oxygen ----------
            if (currentOxygen > 0f)
            {
                currentOxygen = Mathf.Max(0f, currentOxygen - depletionRate * Time.deltaTime);

                if (currentOxygen <= 0f)
                {
                    currentOxygen = 0f;

                    // First time hitting zero?
                    if (!isDepleted)
                    {
                        SetDepletedState(true);
                        onOxygenDepleted?.Invoke();
                    }
                }

                // Check thresholds for sounds while we are still above 0
                float percent = OxygenPercentage;
                if (percent <= intermediateThreshold && !hasPlayedIntermediate)
                {
                    StopSound(lowSound);
                    hasPlayedIntermediate = true;
                    PlaySound(intermediateSound);
                }
                else if (percent <= lowThreshold && !hasPlayedLow && hasPlayedIntermediate)
                {
                    StopSound(intermediateSound);
                    StopSound(depletedSound);
                    hasPlayedLow = true;
                    PlaySound(lowSound);
                }
                else if (percent > lowThreshold && hasPlayedLow)
                {
                    StopSound(lowSound);
                    PlaySound(depletedSound);
                }
            }
            else
            {
                // Already at 0 – ensure depleted state is active
                if (!isDepleted)
                {
                    SetDepletedState(true);
                    onOxygenDepleted?.Invoke();
                }
            }
        }

        UpdateHUD();
    }

    private bool IsPlayerInsideAnyZone()
    {
        Vector3 playerPos = transform.position;
        foreach (var zone in TreeOxygenArea.ActiveZones)
        {
            if (zone == null) continue;
            float radius = zone.OxygenRadius;
            if (radius <= 0f) continue;
            float sqrDist = (playerPos - zone.transform.position).sqrMagnitude;
            if (sqrDist <= radius * radius)
                return true;
        }
        return false;
    }

    private void UpdateHUD()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateOxygen(OxygenPercentage);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXOneShot(clip);
    }

    private void StopSound(AudioClip clip)
        {
        if (clip == null) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopSFX(clip);
    }

    private void ResetSoundFlags()
    {
        hasPlayedIntermediate = false;
        hasPlayedLow = false;
        // Do NOT reset isDepleted here – that's handled separately.
    }

    private void SetDepletedState(bool newState)
    {
        if (isDepleted != newState)
        {
            isDepleted = newState;
            onDepletedStateChanged?.Invoke(isDepleted);
        }
    }

    // Optional: public method to manually refill oxygen
    public void RefillOxygen(float amount)
    {
        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
        if (currentOxygen > 0f)
            SetDepletedState(false);
    }
}