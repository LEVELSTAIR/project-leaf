using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class TreeOxygenArea : MonoBehaviour
{
    [SerializeField] private SeedData seedData;
    private SphereCollider sphereCollider;

    private static List<TreeOxygenArea> activeZones = new List<TreeOxygenArea>();
    public static IReadOnlyList<TreeOxygenArea> ActiveZones => activeZones;

    public float OxygenRadius
    {
        get
        {
            if (seedData != null)
                return seedData.oxygenAreaRadius;
            if (sphereCollider == null)
                sphereCollider = GetComponent<SphereCollider>();
            return sphereCollider != null ? sphereCollider.radius : 0f;
        }
    }

    // Compatibility for older scripts that call GetOxygenRadius()
    public float GetOxygenRadius() => OxygenRadius;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.enabled = false;
    }

    private void OnEnable()
    {
        if (!activeZones.Contains(this))
            activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private void OnDestroy()
    {
        activeZones.Remove(this);
    }

    public void Setup(SeedData data)
    {
        seedData = data;
        sphereCollider.enabled = true;
        sphereCollider.radius = seedData.oxygenAreaRadius;
    }

    private void OnDrawGizmos()
    {
        if (seedData == null || seedData.oxygenAreaRadius <= 0f) return;
        Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.2f);
        Gizmos.DrawSphere(transform.position, seedData.oxygenAreaRadius);
        Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, seedData.oxygenAreaRadius);
    }
}