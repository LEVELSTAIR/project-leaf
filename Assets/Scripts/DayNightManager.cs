using UnityEngine;

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
    public Color dayLightColor = new Color(1f, 0.95f, 0.85f);      // Warm white
    public Color sunriseLightColor = new Color(1f, 0.6f, 0.4f);    // Orange/pink
    public Color sunsetLightColor = new Color(1f, 0.5f, 0.3f);     // Deep orange
    public Color nightLightColor = new Color(0.2f, 0.2f, 0.4f);    // Cool blue

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
    [Tooltip("Sunrise begins at this hour")]
    public float sunriseStartHour = 5f;    // 5:00 AM
    [Tooltip("Sunrise ends / Day fully begins")]
    public float sunriseEndHour = 6f;      // 6:00 AM
    [Tooltip("Full day period")]
    public float dayStartHour = 6f;        // 6:00 AM
    [Tooltip("Sunset begins at this hour")]
    public float sunsetStartHour = 17f;    // 5:00 PM
    [Tooltip("Sunset ends / Night fully begins")]
    public float sunsetEndHour = 18f;      // 6:00 PM
    [Tooltip("Night period begins")]
    public float nightStartHour = 18f;     // 6:00 PM

    [Header("Ambient Light")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f);
    public Color sunriseAmbientColor = new Color(0.3f, 0.2f, 0.2f);
    public Color sunsetAmbientColor = new Color(0.3f, 0.15f, 0.1f);

    [Header("Transition Settings")]
    [Tooltip("How smoothly the light color and intensity blend")]
    public float transitionSmoothness = 2f;

    [Header("Debug")]
    public bool isDay;
    public string formattedTime; // For UI display
    public string currentPeriod; // For debugging

    private float timeScale;
    private Color targetLightColor;
    private float targetIntensity;
    private Color targetAmbientColor;

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

        // 24 in-game hours per full real-time day
        timeScale = 24f / (dayLengthInMinutes * 60f);
        
        // Initialize targets
        UpdateLightingTargets();
        
        // Apply initial lighting immediately
        sunLight.color = targetLightColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.ambientLight = targetAmbientColor;
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

        // Format time for UI (HH:mm)
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        formattedTime = string.Format("{0:00}:{1:00}", hours, minutes);
    }

    void UpdateSunRotation()
    {
        // Map 0–24 hours → 0–360 degrees
        // At 6 AM (sunrise), sun should be at horizon (0°)
        // At 12 PM (noon), sun should be at zenith (90°)
        // At 6 PM (sunset), sun should be at horizon (180°)
        float sunAngle = ((currentTime - 6f) / 24f) * 360f;

        if (sunPivot)
            sunPivot.localRotation = Quaternion.Euler(sunAngle, 0f, 0f);
        else
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
    }

    void UpdateLightingTargets()
    {
        // Determine what period we're in and set target colors/intensity
        
        // NIGHT (6 PM to 5 AM)
        if (currentTime >= nightStartHour || currentTime < sunriseStartHour)
        {
            currentPeriod = "Night";
            targetLightColor = nightLightColor;
            targetIntensity = nightIntensity;
            targetAmbientColor = nightAmbientColor;
        }
        // SUNRISE TRANSITION (5 AM to 6 AM)
        else if (currentTime >= sunriseStartHour && currentTime < sunriseEndHour)
        {
            currentPeriod = "Sunrise";
            float t = (currentTime - sunriseStartHour) / (sunriseEndHour - sunriseStartHour);
            
            // First half: night to sunrise peak
            // Second half: sunrise peak to day
            if (t < 0.5f)
            {
                float subT = t * 2f; // 0 to 1 for first half
                targetLightColor = Color.Lerp(nightLightColor, sunriseLightColor, subT);
                targetIntensity = Mathf.Lerp(nightIntensity, sunriseIntensity, subT);
                targetAmbientColor = Color.Lerp(nightAmbientColor, sunriseAmbientColor, subT);
            }
            else
            {
                float subT = (t - 0.5f) * 2f; // 0 to 1 for second half
                targetLightColor = Color.Lerp(sunriseLightColor, dayLightColor, subT);
                targetIntensity = Mathf.Lerp(sunriseIntensity, dayIntensity, subT);
                targetAmbientColor = Color.Lerp(sunriseAmbientColor, dayAmbientColor, subT);
            }
        }
        // DAY (6 AM to 5 PM)
        else if (currentTime >= dayStartHour && currentTime < sunsetStartHour)
        {
            currentPeriod = "Day";
            targetLightColor = dayLightColor;
            targetIntensity = dayIntensity;
            targetAmbientColor = dayAmbientColor;
        }
        // SUNSET TRANSITION (5 PM to 6 PM)
        else if (currentTime >= sunsetStartHour && currentTime < sunsetEndHour)
        {
            currentPeriod = "Sunset";
            float t = (currentTime - sunsetStartHour) / (sunsetEndHour - sunsetStartHour);
            
            // First half: day to sunset peak
            // Second half: sunset peak to night
            if (t < 0.5f)
            {
                float subT = t * 2f; // 0 to 1 for first half
                targetLightColor = Color.Lerp(dayLightColor, sunsetLightColor, subT);
                targetIntensity = Mathf.Lerp(dayIntensity, sunsetIntensity, subT);
                targetAmbientColor = Color.Lerp(dayAmbientColor, sunsetAmbientColor, subT);
            }
            else
            {
                float subT = (t - 0.5f) * 2f; // 0 to 1 for second half
                targetLightColor = Color.Lerp(sunsetLightColor, nightLightColor, subT);
                targetIntensity = Mathf.Lerp(sunsetIntensity, nightIntensity, subT);
                targetAmbientColor = Color.Lerp(sunsetAmbientColor, nightAmbientColor, subT);
            }
        }
    }

    void ApplyLightingSmooth()
    {
        // Smoothly interpolate current values toward target
        float smoothTime = transitionSmoothness * Time.deltaTime;
        
        sunLight.color = Color.Lerp(sunLight.color, targetLightColor, smoothTime);
        sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, smoothTime);
        sunLight.shadowStrength = Mathf.Clamp01(sunLight.intensity);
        
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbientColor, smoothTime);
    }

    void UpdateSkybox()
    {
        Material targetSkybox = nightSkybox;

        // Determine target skybox based on period
        if (currentTime >= nightStartHour || currentTime < sunriseStartHour)
        {
            targetSkybox = nightSkybox;
        }
        else if (currentTime >= sunriseStartHour && currentTime < sunriseEndHour)
        {
            targetSkybox = sunriseSkybox ? sunriseSkybox : daySkybox;
        }
        else if (currentTime >= dayStartHour && currentTime < sunsetStartHour)
        {
            targetSkybox = daySkybox;
        }
        else if (currentTime >= sunsetStartHour && currentTime < sunsetEndHour)
        {
            targetSkybox = sunsetSkybox ? sunsetSkybox : nightSkybox;
        }

        if (RenderSettings.skybox != targetSkybox && targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment(); // Update lighting reflections
        }
    }

    void UpdateDayNightState()
    {
        isDay = currentTime >= sunriseEndHour && currentTime < sunsetEndHour;
    }

    void UpdateHUDClock()
    {
        // Update HUD clock if HUDManager exists
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateTime(formattedTime);
        }
    }

    public string GetFormattedTime()
    {
        return formattedTime;
    }

    public string GetCurrentPeriod()
    {
        return currentPeriod;
    }

    /// <summary>
    /// Returns a normalized value (0-1) representing how much daylight there is.
    /// 0 = full night, 1 = full day
    /// </summary>
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

    /// <summary>
    /// Sets the time directly (useful for debugging or time-skip features)
    /// </summary>
    public void SetTime(float hour)
    {
        currentTime = Mathf.Clamp(hour, 0f, 24f);
        UpdateLightingTargets();
        
        // Apply immediately when time is set manually
        sunLight.color = targetLightColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.ambientLight = targetAmbientColor;
    }
}
