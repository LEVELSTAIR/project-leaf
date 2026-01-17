using UnityEngine;

public class DayNightManager : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Length of a full day in real-time minutes")]
    public float dayLengthInMinutes = 20f;

    [Range(0f, 24f)]
    public float currentTime = 6f; // Start at 6 AM

    [Header("Sun Settings")]
    public Light sunLight;
    public Transform sunPivot; // Usually an empty GameObject

    [Header("Skybox Settings")]
    public Material daySkybox;
    public Material nightSkybox;
    public Material sunriseSkybox;
    public Material sunsetSkybox;

    [Header("Day/Night Thresholds")]
    public float sunriseHour = 6f;
    public float dayStartHour = 8f;
    public float sunsetHour = 18f;
    public float nightStartHour = 20f;

    [Header("Debug")]
    public bool isDay;
    public string formattedTime; // For UI display

    private float timeScale;

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
    }

    void Update()
    {
        AdvanceTime();
        UpdateSunRotation();
        UpdateSkybox();
        UpdateDayNightState();
    }

    void AdvanceTime()
    {
        currentTime += Time.deltaTime * timeScale;

        if (currentTime >= 24f)
            currentTime -= 24f;

        // Format time for UI (HH:mm)
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        formattedTime = string.Format("{0:00}:{1:00}", hours, minutes);
    }

    void UpdateSunRotation()
    {
        // Map 0–24 hours → 0–360 degrees
        float sunAngle = (currentTime / 24f) * 360f - 90f;

        if (sunPivot)
            sunPivot.localRotation = Quaternion.Euler(sunAngle, 0f, 0f);
        else
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);
    }

    void UpdateSkybox()
    {
        // Simple state machine for skybox switching
        Material targetSkybox = nightSkybox;

        if (currentTime >= sunriseHour && currentTime < dayStartHour)
        {
            targetSkybox = sunriseSkybox ? sunriseSkybox : daySkybox;
        }
        else if (currentTime >= dayStartHour && currentTime < sunsetHour)
        {
            targetSkybox = daySkybox;
        }
        else if (currentTime >= sunsetHour && currentTime < nightStartHour)
        {
            targetSkybox = sunsetSkybox ? sunsetSkybox : nightSkybox;
        }
        else
        {
            targetSkybox = nightSkybox;
        }

        if (RenderSettings.skybox != targetSkybox && targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment(); // Update lighting reflections
        }
    }

    void UpdateDayNightState()
    {
        isDay = currentTime >= sunriseHour && currentTime < nightStartHour;

        // Optional: simple intensity change based on time
        if (isDay)
        {
             sunLight.intensity = 1f;
             sunLight.shadowStrength = 1f;
        }
        else
        {
            sunLight.intensity = 0.1f;
            sunLight.shadowStrength = 0.1f;
        }
    }

    public string GetFormattedTime()
    {
        return formattedTime;
    }
}
