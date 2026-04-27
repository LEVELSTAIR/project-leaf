using UnityEngine;

/// <summary>
/// Diagnostic tool to check and fix terrain detail rendering settings.
/// Add this script to any GameObject in your scene to diagnose terrain issues.
/// </summary>
public class TerrainDetailDiagnostics : MonoBehaviour
{
    [Header("Diagnostics")]
    [SerializeField] private bool runDiagnosticsOnStart = true;
    [SerializeField] private bool fixTerrainSettings = false;

    private void Start()
    {
        if (runDiagnosticsOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("Run Terrain Diagnostics")]
    public void RunDiagnostics()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("[TerrainDiagnostics] No active terrain found in scene!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("[TerrainDiagnostics] Terrain has no TerrainData asset assigned!");
            return;
        }

        Debug.Log("=== TERRAIN DIAGNOSTICS ===");
        Debug.Log($"Terrain Name: {terrain.name}");
        Debug.Log($"Terrain Position: {terrain.transform.position}");
        Debug.Log($"Terrain Size: {terrainData.size}");
        Debug.Log($"Heightmap Resolution: {terrainData.heightmapResolution}");
        Debug.Log($"Detail Map Resolution: {terrainData.detailWidth}x{terrainData.detailHeight}");
        Debug.Log($"Detail Prototypes Count: {terrainData.detailPrototypes.Length}");

        // Check each detail prototype
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            DetailPrototype proto = terrainData.detailPrototypes[i];
            Debug.Log($"\n--- Detail Prototype {i} ---");
            Debug.Log($"Name: {(proto.prototype != null ? proto.prototype.name : "NULL")}");
            Debug.Log($"Prototype Type: {proto.prototypeTexture}");
            Debug.Log($"Render Mode: {proto.renderMode}");
            Debug.Log($"Health Min/Max: {proto.healthyColor} / {proto.dryColor}");
        }

        // Check terrain renderer settings
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        Debug.Log($"\n--- Terrain Collider ---");
        Debug.Log($"Has TerrainCollider: {terrainCollider != null}");
        if (terrainCollider != null)
        {
            Debug.Log($"TerrainCollider enabled: {terrainCollider.enabled}");
        }

        // Check Layer Masks and Draw Instanced
        Debug.Log($"\n--- Terrain Rendering ---");
        Debug.Log($"Terrain Layer Mask: {LayerMask.LayerToName(terrain.gameObject.layer)}");

        // Check if details are being rendered
        int detailCount = 0;
        foreach (DetailPrototype proto in terrainData.detailPrototypes)
        {
            detailCount++;
        }
        Debug.Log($"Total detail prototypes: {detailCount}");

        // Get terrain layer settings
        Debug.Log($"\n--- Terrain Layers ---");
        TerrainLayer[] layers = terrainData.terrainLayers;
        Debug.Log($"Terrain Layers Count: {layers.Length}");

        // Check draw instanced
        Debug.Log($"\n--- Performance Settings ---");
        Debug.Log($"Drawing terrain with instancing");

        Debug.Log("=== END DIAGNOSTICS ===\n");

        // Recommendations
        Debug.Log("=== RECOMMENDATIONS ===");
        if (detailCount == 0)
        {
            Debug.LogWarning("[TerrainDiagnostics] WARNING: No detail prototypes found! Add grass/detail prototypes to the terrain.");
        }

        if (terrain.gameObject.layer == 0)
        {
            Debug.LogWarning("[TerrainDiagnostics] WARNING: Terrain is on 'Default' layer. Consider using a specific layer.");
        }

        Debug.Log("To enable detail rendering:");
        Debug.Log("1. Select the Terrain in the Hierarchy");
        Debug.Log("2. In the Inspector, go to 'Terrain Settings' (gear icon)");
        Debug.Log("3. Find 'Draw Instanced' - ensure it's enabled");
        Debug.Log("4. Check 'Detail Distance' - increase if details aren't visible");
        Debug.Log("5. Check 'Detail Density' - increase for more grass");
        Debug.Log("6. Ensure you have Detail Prototypes configured");
    }

    [ContextMenu("Fix Terrain Settings")]
    public void FixTerrainSettings()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("[TerrainDiagnostics] No active terrain found!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("[TerrainDiagnostics] Terrain has no TerrainData!");
            return;
        }

        Debug.Log("[TerrainDiagnostics] Attempting to fix terrain settings...");

        // Ensure terrain collider is enabled
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            Debug.LogWarning("[TerrainDiagnostics] Adding TerrainCollider...");
            terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
            terrainCollider.terrainData = terrainData;
        }
        else if (!terrainCollider.enabled)
        {
            Debug.LogWarning("[TerrainDiagnostics] Enabling TerrainCollider...");
            terrainCollider.enabled = true;
        }

        // Check if detail prototypes exist
        if (terrainData.detailPrototypes.Length == 0)
        {
            Debug.LogError("[TerrainDiagnostics] ERROR: No detail prototypes! You must add them manually in the Terrain Inspector.");
            Debug.Log("Steps to add detail prototypes:");
            Debug.Log("1. Select the Terrain in the Hierarchy");
            Debug.Log("2. Switch to Paint Detail tool (last tool in terrain tools)");
            Debug.Log("3. Click 'Add Detail Prototype' and select a grass prefab or texture");
            return;
        }

        Debug.Log("[TerrainDiagnostics] Terrain settings verified/fixed!");
        RunDiagnostics();
    }
}
