using UnityEngine;

public class CageTrap : MonoBehaviour
{
    [Header("Capture Settings")]
    [Tooltip("If true, the cage will be destroyed after capturing a tree.")]
    public bool destroyOnCapture = true;

    [Tooltip("Optional: play a capture effect (particle system).")]
    public ParticleSystem captureEffect;

    [Tooltip("Optional: tag used to find the player or other target.")]
    public string evilTreeTag = "EvilTree";   // Make sure your Evil Tree has this tag

    private bool hasCaptured = false;

    private void Start()
    {
        // When the cage is placed (instantiated), try to capture any tree inside its trigger.
        CaptureTreeInside();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Also allow capture if the tree walks into the cage after placement.
        if (!hasCaptured && other.CompareTag(evilTreeTag))
        {
            TryCapture(other.gameObject);
        }
    }

    private void CaptureTreeInside()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, GetCageRadius());
        foreach (Collider col in colliders)
        {
            if (col.CompareTag(evilTreeTag))
            {
                TryCapture(col.gameObject);
                break; // Capture only one tree per cage (optional)
            }
        }
    }

    private void TryCapture(GameObject treeObject)
    {
        if (hasCaptured) return;

        EvilTree evilTree = treeObject.GetComponent<EvilTree>();
        if (evilTree != null)
        {
            evilTree.Capture(); // Call capture method on the tree

            // Play capture effect
            if (captureEffect != null)
            {
                Instantiate(captureEffect, treeObject.transform.position, Quaternion.identity);
            }

            hasCaptured = true;

            // Optional: disable cage collider so it doesn't capture again
            GetComponent<Collider>().enabled = true;

            if (destroyOnCapture)
            {
                Destroy(gameObject, 0.5f); // slight delay to allow effect
            }
        }
    }

    private float GetCageRadius()
    {
        // Estimate the cage's size – you can also use the trigger's bounds.
        Collider col = GetComponent<Collider>();
        if (col != null)
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        else
            return 1.5f; // default radius
    }

    // Optional: draw gizmo to visualise capture radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, GetCageRadius());
    }
}