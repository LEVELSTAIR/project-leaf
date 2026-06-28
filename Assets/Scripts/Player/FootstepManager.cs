using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController controller; // Your FPC
    [SerializeField] private Transform footRayOrigin; // Optional: where raycast originates. If null, uses player position.

    [Header("Footstep Sound Sets")]
    [SerializeField] private AudioClip[] grassFootsteps;
    [SerializeField] private AudioClip[] snowFootsteps;
    [SerializeField] private AudioClip[] desertFootsteps;
    [SerializeField] private AudioClip[] lushFootsteps;   // e.g., forest/grass with moisture
    [SerializeField] private AudioClip[] defaultFootsteps; // fallback

    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float raycastDistance = 1.5f;

    [Header("Step Timing")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;

    [Header("Volume & Pitch")]
    [SerializeField] private float footstepVolume = 0.5f;
    [Range(0.8f, 1.2f)]
    [SerializeField] private float pitchVariation = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Private
    private float stepTimer = 0f;
    private Terrain terrain;
    private int alphamapWidth;
    private int alphamapHeight;
    private float[,,] alphamapCache; // cache the alphamap for performance

    private void Start()
    {
        // Auto-find controller if not assigned
        if (controller == null)
            controller = GetComponent<FirstPersonController>();

        if (controller == null)
            Debug.LogError("FootstepManager: No FirstPersonController found! Please assign one.");

        if (footRayOrigin == null)
            footRayOrigin = transform;

        // Cache terrain reference
        terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            // Try find by tag if activeTerrain is null
            GameObject terrainObj = GameObject.FindGameObjectWithTag("Terrain");
            if (terrainObj != null)
                terrain = terrainObj.GetComponent<Terrain>();
        }

        if (terrain != null)
        {
            TerrainData data = terrain.terrainData;
            alphamapWidth = data.alphamapResolution;
            alphamapHeight = data.alphamapResolution;
            // We'll read alphamap on demand, but we can cache it each time we sample
            // to avoid repeated reads we could update every few frames, but we'll keep it simple.
        }
        else
        {
            Debug.LogWarning("FootstepManager: No Terrain found. Will use tag/layer detection only.");
        }
    }

    private void Update()
    {
        if (controller == null) return;

        // Use public bools from FirstPersonController (make them public in that script)
        bool isWalking = controller.isWalking;
        bool isSprinting = controller.isSprinting;
        bool isCrouched = controller.isCrouched;

        // Alternatively, detect movement via velocity if the above are not public.
        // We'll use the public approach assuming we've exposed them.

        Rigidbody rb = controller.GetComponent<Rigidbody>();
        bool isActuallyMoving = false;
        if (rb != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            isActuallyMoving = horizontalVelocity.magnitude > 0.1f;
        }

        if (isActuallyMoving && isWalking)
        {
            float interval = walkStepInterval;
            if (isSprinting) interval = sprintStepInterval;
            else if (isCrouched) interval = crouchStepInterval;

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayFootstep(isSprinting, isCrouched);
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayFootstep(bool isSprinting, bool isCrouched)
    {
        // Determine surface type
        string surfaceType = DetectSurfaceType();

        // Select appropriate clip array
        AudioClip[] clips = GetClipsForSurface(surfaceType, isSprinting, isCrouched);

        if (clips == null || clips.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"FootstepManager: No clips for surface '{surfaceType}'.");
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip, footstepVolume, pitch, false);
            if (showDebugLogs)
                Debug.Log($"Footstep: {clip.name} on {surfaceType} (pitch: {pitch:F2})");
        }
        else
        {
            // Fallback
            AudioSource.PlayClipAtPoint(clip, footRayOrigin.position, footstepVolume);
        }
    }

    private string DetectSurfaceType()
    {
        RaycastHit hit;
        Vector3 origin = footRayOrigin.position;
        if (Physics.Raycast(origin, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            // Check if we hit a Terrain
            Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
            if (hitTerrain != null)
            {
                // Sample terrain alphamap to get dominant layer
                return GetTerrainSurfaceType(hit.point);
            }
            else
            {
                // Use tag or name for non-terrain objects
                string tag = hit.collider.tag;
                if (!string.IsNullOrEmpty(tag) && tag != "Untagged")
                    return tag;
                else
                    return hit.collider.gameObject.name;
            }
        }
        return "Default";
    }

    private string GetTerrainSurfaceType(Vector3 worldPos)
    {
        if (terrain == null) return "Default";

        TerrainData data = terrain.terrainData;
        // Convert world position to terrain UV
        Vector3 terrainPos = terrain.transform.position;
        Vector3 size = data.size;
        float u = (worldPos.x - terrainPos.x) / size.x;
        float v = (worldPos.z - terrainPos.z) / size.z;
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        // Sample alphamap at this UV (needs to be in pixel coordinates)
        int pixelX = Mathf.RoundToInt(u * (alphamapWidth - 1));
        int pixelY = Mathf.RoundToInt(v * (alphamapHeight - 1));

        // Get alphamap (3D array [y, x, layer])
        // We'll read only the needed pixel for performance
        float[,,] alpha = data.GetAlphamaps(pixelX, pixelY, 1, 1);
        int layerCount = data.terrainLayers.Length;

        // Find dominant layer
        int dominantIndex = 0;
        float maxWeight = 0f;
        for (int i = 0; i < layerCount; i++)
        {
            float w = alpha[0, 0, i];
            if (w > maxWeight)
            {
                maxWeight = w;
                dominantIndex = i;
            }
        }

        // Map layer index to surface type string
        // We assume the order of terrain layers is: Grass(0), Snow(1), Desert(2), Lush(3)
        // but you can customize this mapping.
        switch (dominantIndex)
        {
            case 0: return "Grass";
            case 1: return "Snow";
            case 2: return "Desert";
            case 3: return "Lush";
            default: return "Default";
        }
    }

    private AudioClip[] GetClipsForSurface(string surface, bool isSprinting, bool isCrouched)
    {
        // You can customize which array to use based on surface name
        // For simplicity, we use the same array for walk/sprint/crouch, but you could split them.
        switch (surface)
        {
            case "Grass": return grassFootsteps;
            case "Snow": return snowFootsteps;
            case "Desert": return desertFootsteps;
            case "Lush": return lushFootsteps;
            default: return defaultFootsteps;
        }
    }
}