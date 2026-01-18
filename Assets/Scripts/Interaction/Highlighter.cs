using UnityEngine;

public class Highlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1.2f;

    private Renderer meshRenderer;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private bool isHighlighted = false;

    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            originalMaterials = meshRenderer.materials;
            highlightMaterials = new Material[originalMaterials.Length];
            
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                // Create a temporary material instance to modify
                // Ideally, use a shader property like "_EmissionColor" or "_Outline" instead of swapping materials
                // For this implementation, we'll assume a standard shader and bump emission.
                highlightMaterials[i] = new Material(originalMaterials[i]);
                highlightMaterials[i].EnableKeyword("_EMISSION");
            }
        }
    }

    public void SetHighlight(bool active)
    {
        if (isHighlighted == active || meshRenderer == null) return;

        isHighlighted = active;

        if (active)
        {
            for (int i = 0; i < highlightMaterials.Length; i++)
            {
                highlightMaterials[i].SetColor("_EmissionColor", highlightColor * highlightIntensity);
            }
            meshRenderer.materials = highlightMaterials;
        }
        else
        {
            meshRenderer.materials = originalMaterials;
        }
    }
}
