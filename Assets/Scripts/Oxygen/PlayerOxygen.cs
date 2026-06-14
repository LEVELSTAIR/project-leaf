using UnityEngine;
using UnityEngine.Events;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float depletionRate = 5f;
    [SerializeField] private float refillRate = 10f;

    [Header("Events")]
    public UnityEvent onOxygenDepleted;

    private float currentOxygen;

    public float OxygenPercentage => currentOxygen / maxOxygen;
    public bool IsInOxygenZone { get; private set; }

    private void Start()
    {
        currentOxygen = maxOxygen;
        UpdateHUD();
    }

    private void Update()
    {
        IsInOxygenZone = IsPlayerInsideAnyZone();

        if (IsInOxygenZone)
        {
            if (refillRate > 0f)
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + refillRate * Time.deltaTime);
            else
                currentOxygen = maxOxygen;
        }
        else
        {
            if (currentOxygen > 0f)
            {
                currentOxygen = Mathf.Max(0f, currentOxygen - depletionRate * Time.deltaTime);
                if (currentOxygen <= 0f)
                    onOxygenDepleted?.Invoke();
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
}