using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TreeOxygenArea : MonoBehaviour
{
    [SerializeField] private SeedData seedData;
    private SphereCollider sphereCollider;

    /// <summary>Get the current oxygen area radius.</summary>
    public float GetOxygenRadius()
    {
        if (seedData != null)
            return seedData.oxygenAreaRadius;

        if (sphereCollider == null)
            sphereCollider = GetComponent<SphereCollider>();

        return sphereCollider != null ? sphereCollider.radius : 0f;
    }

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.enabled = false;

        // Ensure the marker is present (it will be added automatically if not)
        if (!TryGetComponent<OxygenZoneMarker>(out _))
            gameObject.AddComponent<OxygenZoneMarker>();
    }

    /// <summary>Call this when the tree reaches full maturity.</summary>
    public void Setup(SeedData data)
    {
        seedData = data;
        sphereCollider.enabled = true;
        sphereCollider.radius = seedData.oxygenAreaRadius;
    }

    private void OnValidate()
    {
        if (seedData != null && TryGetComponent(out sphereCollider))
            sphereCollider.radius = seedData.oxygenAreaRadius;
    }

    // ---- Gizmo ----
    private void OnDrawGizmos()
    {
        if (seedData == null || seedData.oxygenAreaRadius <= 0f) return;
        Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.2f);
        Gizmos.DrawSphere(transform.position, seedData.oxygenAreaRadius);
        Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, seedData.oxygenAreaRadius);
    }
}