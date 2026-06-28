using UnityEngine;
using System.Collections;

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("Length of a full day in real-time minutes")]
    public float dayLengthInMinutes = 20f;

    [Range(0f, 24f)]
    public float currentTime = 6f; // Start at 6 AM

    [Header("Sun Settings")]
    public Light sunLight;
    public Transform sunPivot; // Usually an empty GameObject

    [Header("Lighting Colors")]
    public Color dayLightColor = new Color(1f, 0.95f, 0.85f);
    public Color sunriseLightColor = new Color(1f, 0.6f, 0.4f);
    public Color sunsetLightColor = new Color(1f, 0.5f, 0.3f);
    public Color nightLightColor = new Color(0.2f, 0.2f, 0.4f);

    [Header("Lighting Intensity")]
    public float dayIntensity = 1.2f;
    public float sunriseIntensity = 0.6f;
    public float sunsetIntensity = 0.5f;
    public float nightIntensity = 0.05f;

    [Header("Skybox Settings")]
    public Material daySkybox;
    public Material nightSkybox;
    public Material sunriseSkybox;
    public Material sunsetSkybox;

    [Header("Day/Night Thresholds")]
    public float sunriseStartHour = 5f;
    public float sunriseEndHour = 6f;
    public float dayStartHour = 6f;
    public float sunsetStartHour = 17f;
    public float sunsetEndHour = 18f;
    public float nightStartHour = 18f;

    [Header("Ambient Light")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f);
    public Color sunriseAmbientColor = new Color(0.3f, 0.2f, 0.2f);
    public Color sunsetAmbientColor = new Color(0.3f, 0.15f, 0.1f);

    [Header("Transition Settings")]
    public float transitionSmoothness = 2f;

    // ========== NEW MUSIC SETTINGS ==========
    [Header("Music")]
    public AudioClip dayMusic;
    public AudioClip nightMusic;
    public AudioClip sunriseMusic;
    public AudioClip sunsetMusic;
    [Tooltip("Duration (seconds) to crossfade between music tracks. Set to 0 for instant switch.")]
    public float musicCrossfadeDuration = 3f;
    public float musicVolume = 0.7f;

    [Header("Debug")]
    public bool isDay;
    public string formattedTime; // For UI display
    public string currentPeriod; // For debugging

    private float timeScale;
    private Color targetLightColor;
    private float targetIntensity;
    private Color targetAmbientColor;

    // ========== NEW MUSIC VARIABLES ==========
    private string currentMusicPeriod = ""; // Tracks last played period to avoid redundant switches
    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (!sunLight)
        {
            Debug.LogError("DayNightManager: Sun Light not assigned.");
            enabled = false;
            return;
        }

        timeScale = 24f / (dayLengthInMinutes * 60f);

        UpdateLightingTargets();
        sunLight.color = targetLightColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.ambientLight = targetAmbientColor;

        // Set initial music
        UpdateMusic();
    }

    void Update()
    {
        AdvanceTime();
        UpdateSunRotation();
        UpdateLightingTargets();
        ApplyLightingSmooth();
        UpdateSkybox();
        UpdateDayNightState();
        UpdateHUDClock();

        // ========== CHECK FOR MUSIC CHANGE ==========
        UpdateMusic();
    }

    void AdvanceTime()
    {
        float deltaGameHours = Time.deltaTime * timeScale;
        currentTime += deltaGameHours;

        if (currentTime >= 24f)
            currentTime -= 24f;

        // Advance plant growth
        if (ProjectLeaf.Garden.PlantManager.Instance != null)
        {
            ProjectLeaf.Garden.PlantManager.Instance.AdvanceGrowth(deltaGameHours);
        }

        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        formattedTime = string.Format("{0:00}:{1:00}", hours, minutes);
    }

    void UpdateSunRotation()
    {
        float sunAngle = ((currentTime - 6f) / 24f) * 360f;

        if (sunPivot)
            sunPivot.localRotation = Quaternion.Euler(sunAngle, 0f, 0f);
        else
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
    }

    void UpdateLightingTargets()
    {
        if (currentTime >= nightStartHour || currentTime < sunriseStartHour)
        {
            currentPeriod = "Night";
            targetLightColor = nightLightColor;
            targetIntensity = nightIntensity;
            targetAmbientColor = nightAmbientColor;
        }
        else if (currentTime >= sunriseStartHour && currentTime < sunriseEndHour)
        {
            currentPeriod = "Sunrise";
            float t = (currentTime - sunriseStartHour) / (sunriseEndHour - sunriseStartHour);
            if (t < 0.5f)
            {
                float subT = t * 2f;
                targetLightColor = Color.Lerp(nightLightColor, sunriseLightColor, subT);
                targetIntensity = Mathf.Lerp(nightIntensity, sunriseIntensity, subT);
                targetAmbientColor = Color.Lerp(nightAmbientColor, sunriseAmbientColor, subT);
            }
            else
            {
                float subT = (t - 0.5f) * 2f;
                targetLightColor = Color.Lerp(sunriseLightColor, dayLightColor, subT);
                targetIntensity = Mathf.Lerp(sunriseIntensity, dayIntensity, subT);
                targetAmbientColor = Color.Lerp(sunriseAmbientColor, dayAmbientColor, subT);
            }
        }
        else if (currentTime >= dayStartHour && currentTime < sunsetStartHour)
        {
            currentPeriod = "Day";
            targetLightColor = dayLightColor;
            targetIntensity = dayIntensity;
            targetAmbientColor = dayAmbientColor;
        }
        else if (currentTime >= sunsetStartHour && currentTime < sunsetEndHour)
        {
            currentPeriod = "Sunset";
            float t = (currentTime - sunsetStartHour) / (sunsetEndHour - sunsetStartHour);
            if (t < 0.5f)
            {
                float subT = t * 2f;
                targetLightColor = Color.Lerp(dayLightColor, sunsetLightColor, subT);
                targetIntensity = Mathf.Lerp(dayIntensity, sunsetIntensity, subT);
                targetAmbientColor = Color.Lerp(dayAmbientColor, sunsetAmbientColor, subT);
            }
            else
            {
                float subT = (t - 0.5f) * 2f;
                targetLightColor = Color.Lerp(sunsetLightColor, nightLightColor, subT);
                targetIntensity = Mathf.Lerp(sunsetIntensity, nightIntensity, subT);
                targetAmbientColor = Color.Lerp(sunsetAmbientColor, nightAmbientColor, subT);
            }
        }
    }

    void ApplyLightingSmooth()
    {
        float smoothTime = transitionSmoothness * Time.deltaTime;

        sunLight.color = Color.Lerp(sunLight.color, targetLightColor, smoothTime);
        sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, smoothTime);
        sunLight.shadowStrength = Mathf.Clamp01(sunLight.intensity);

        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbientColor, smoothTime);
    }

    void UpdateSkybox()
    {
        Material targetSkybox = nightSkybox;

        if (currentTime >= nightStartHour || currentTime < sunriseStartHour)
            targetSkybox = nightSkybox;
        else if (currentTime >= sunriseStartHour && currentTime < sunriseEndHour)
            targetSkybox = sunriseSkybox ? sunriseSkybox : daySkybox;
        else if (currentTime >= dayStartHour && currentTime < sunsetStartHour)
            targetSkybox = daySkybox;
        else if (currentTime >= sunsetStartHour && currentTime < sunsetEndHour)
            targetSkybox = sunsetSkybox ? sunsetSkybox : nightSkybox;

        if (RenderSettings.skybox != targetSkybox && targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    void UpdateDayNightState()
    {
        isDay = currentTime >= sunriseEndHour && currentTime < sunsetEndHour;
    }

    void UpdateHUDClock()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateTime(formattedTime);
    }

    // ===================== NEW MUSIC LOGIC =====================

    /// <summary>
    /// Checks if the time period has changed and updates the background music accordingly.
    /// </summary>
    private void UpdateMusic()
    {
        if (SoundManager.Instance == null) return;

        string newPeriod = currentPeriod;
        if (newPeriod == currentMusicPeriod) return;
        currentMusicPeriod = newPeriod;

        AudioClip clipToPlay = null;

        switch (newPeriod)
        {
            case "Day": clipToPlay = dayMusic; break;
            case "Night": clipToPlay = nightMusic; break;
            case "Sunrise": clipToPlay = sunriseMusic != null ? sunriseMusic : dayMusic; break;
            case "Sunset": clipToPlay = sunsetMusic != null ? sunsetMusic : nightMusic; break;
            default: return;
        }

        if (clipToPlay == null)
        {
            Debug.LogWarning($"DayNightManager: No music assigned for '{newPeriod}'.");
            return;
        }

        // Use SoundManager's crossfade method
        SoundManager.Instance.CrossfadeMusic(clipToPlay, musicVolume, musicCrossfadeDuration);

        Debug.Log($"DayNightManager: Changed music to '{clipToPlay.name}' for '{newPeriod}'.");
    }

    // ===================== END MUSIC LOGIC =====================

    public string GetFormattedTime() => formattedTime;
    public string GetCurrentPeriod() => currentPeriod;

    public float GetDaylightAmount()
    {
        if (currentTime >= nightStartHour || currentTime < sunriseStartHour)
            return 0f;
        else if (currentTime >= dayStartHour && currentTime < sunsetStartHour)
            return 1f;
        else if (currentTime >= sunriseStartHour && currentTime < sunriseEndHour)
            return (currentTime - sunriseStartHour) / (sunriseEndHour - sunriseStartHour);
        else if (currentTime >= sunsetStartHour && currentTime < sunsetEndHour)
            return 1f - ((currentTime - sunsetStartHour) / (sunsetEndHour - sunsetStartHour));

        return 0.5f;
    }

    public void SetTime(float hour)
    {
        currentTime = Mathf.Clamp(hour, 0f, 24f);
        UpdateLightingTargets();
        sunLight.color = targetLightColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.ambientLight = targetAmbientColor;
        // Force music update immediately after time change
        currentMusicPeriod = ""; // reset to force update
        UpdateMusic();
    }

    public float GetCurrentHour() => currentTime;
}