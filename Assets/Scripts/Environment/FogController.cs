using UnityEngine;

public class FogController : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public FogMode fogMode = FogMode.Exponential;

    [Header("Density by Time")]
    public float nightFogDensity = 0.02f;
    public float sunriseFogDensity = 0.01f;
    public float dayFogDensity = 0.005f;
    public float sunsetFogDensity = 0.015f;

    [Header("Color by Time")]
    public Color nightFogColor = new Color(0.1f, 0.1f, 0.2f);     // Dark blue
    public Color sunriseFogColor = new Color(0.8f, 0.5f, 0.3f);   // Orange/pink
    public Color dayFogColor = new Color(0.6f, 0.7f, 0.8f);       // Light blue-gray
    public Color sunsetFogColor = new Color(0.9f, 0.4f, 0.2f);    // Deep orange/red

    [Header("Transition")]
    public float transitionSpeed = 2f;

    private float targetDensity;
    private Color targetColor;

    void Start()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = fogMode;

        UpdateTargets(); // initial calculation
        RenderSettings.fogDensity = targetDensity;
        RenderSettings.fogColor = targetColor;
    }

    void Update()
    {
        if (!enableFog) return;

        UpdateTargets();

        float smoothDelta = transitionSpeed * Time.deltaTime;
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, smoothDelta);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, smoothDelta);
    }

    private void UpdateTargets()
    {
        float currentHour = DayNightManager.Instance.GetCurrentHour();

        // NIGHT: 6 PM – 5 AM
        if (currentHour >= 18f || currentHour < 5f)
        {
            targetDensity = nightFogDensity;
            targetColor = nightFogColor;
        }
        // SUNRISE: 5 AM – 6 AM
        else if (currentHour >= 5f && currentHour < 6f)
        {
            float t = (currentHour - 5f) / 1f; // 0 → 1 over 1 hour
            targetDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, t);
            targetColor = Color.Lerp(nightFogColor, dayFogColor, t);
        }
        // DAY: 6 AM – 5 PM
        else if (currentHour >= 6f && currentHour < 17f)
        {
            targetDensity = dayFogDensity;
            targetColor = dayFogColor;
        }
        // SUNSET: 5 PM – 6 PM
        else if (currentHour >= 17f && currentHour < 18f)
        {
            float t = (currentHour - 17f) / 1f; // 0 → 1 over 1 hour
            targetDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, t);
            targetColor = Color.Lerp(dayFogColor, nightFogColor, t);
        }
    }
}