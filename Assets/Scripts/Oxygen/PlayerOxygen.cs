using UnityEngine;
using UnityEngine.Events;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float depletionRate = 5f;   // units per second
    [SerializeField] private float refillRate = 10f;     // units per second (set to 0 for instant refill)

    [Header("Events")]
    public UnityEvent onOxygenDepleted;   // e.g. damage or kill the player

    private int zoneCount = 0;            // how many oxygen zones the player is currently inside
    private float currentOxygen;

    public float OxygenPercentage => currentOxygen / maxOxygen;
    public bool IsInOxygenZone => zoneCount > 0;

    private void Start()
    {
        currentOxygen = maxOxygen;
        UpdateHUD();
    }

    private void Update()
    {
        if (IsInOxygenZone)
        {
            if (refillRate > 0f)
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + refillRate * Time.deltaTime);
            else
                currentOxygen = maxOxygen;   // instant refill
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

    private void UpdateHUD()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateOxygen(OxygenPercentage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<OxygenZoneMarker>(out _))
            zoneCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<OxygenZoneMarker>(out _))
        {
            zoneCount--;
            if (zoneCount < 0) zoneCount = 0;
        }
    }
}
