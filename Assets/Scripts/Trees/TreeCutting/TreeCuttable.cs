using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TreeCuttable : MonoBehaviour
{
    [Header("Wood Reward")]
    public int woodAmount = 10;
    public string woodItemName = "Wood";

    [Header("Hit Settings")]
    public int hitsToCut = 1;
    private int currentHits;

    [Header("Respawn")]
    public bool respawnAfterDelay = false;
    public float respawnDelay = 300f;

    [Header("Visual Object to Disable")]
    public GameObject treeVisual;          // Assign the LODS GameObject here

    [Header("Visual & Audio")]
    public ParticleSystem cutEffect;
    public AudioClip cutSound;
    public GameObject stumpObject;

    [Header("Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeAmount = 0.05f;

    [Header("Axe Animation")]
    public string chopBoolName = "IsChopping";
    public float chopAnimationDuration = 1.0f;

    [Header("Tree State")]
    public bool isCutDowned;
    public int currentHitsHitted;
    public float respawnTimeRemaining;

    private bool isCutDown = false;
    private bool isChopping = false;
    private float respawnTimerRemaining = 0f;
    private Collider treeCollider;
    private bool isRespawning = false;   // Prevents duplicate respawn coroutines

    // ---- Public getters for other systems ----
    public bool IsCutDown => isCutDown;
    public int CurrentHits => currentHits;
    public float RespawnTimeRemaining => respawnTimerRemaining;

    // ---- MonoBehaviour ----
    private void OnGUI()
    {
        // Update public fields for display in inspector
        isCutDowned = isCutDown;
        currentHitsHitted = currentHits;
        respawnTimeRemaining = respawnTimerRemaining;
    }

    private void Start()
    {
        currentHits = hitsToCut;
        treeCollider = GetComponent<Collider>();
        FindVisualIfMissing();   // auto-assign if field is empty

        // Apply current state (respects isCutDown)
        if (treeVisual != null)
            treeVisual.SetActive(!isCutDown);
        if (treeCollider != null)
            treeCollider.enabled = !isCutDown;
        if (stumpObject != null)
            stumpObject.SetActive(isCutDown);

        // If already cut down and respawn is pending, resume timer
        if (isCutDown && respawnAfterDelay && respawnTimerRemaining > 0f && !isRespawning)
        {
            isRespawning = true;
            StartCoroutine(ResumeRespawnTimer());
        }

        if (treeVisual == null)
            Debug.LogError("TreeCuttable: treeVisual is not assigned and could not be auto-found!", this);
    }

    // ---- Public method for state restoration (called by TreeUnifiedInteraction) ----
    public void SetCuttableState(bool cutDown, int hits, float respawnRemaining)
    {
        isCutDown = cutDown;
        currentHits = Mathf.Clamp(hits, 0, hitsToCut);
        respawnTimerRemaining = Mathf.Max(0, respawnRemaining);

        FindVisualIfMissing();

        // Update visuals and collider
        if (treeVisual != null)
            treeVisual.SetActive(!isCutDown);
        if (treeCollider != null)
            treeCollider.enabled = !isCutDown;
        if (stumpObject != null)
            stumpObject.SetActive(isCutDown);

        // Start respawn timer if needed
        if (isCutDown && respawnAfterDelay && respawnTimerRemaining > 0f && !isRespawning)
        {
            isRespawning = true;
            StartCoroutine(ResumeRespawnTimer());
        }
    }

    // ---- Respawn timer continuation (used after loading) ----
    private IEnumerator ResumeRespawnTimer()
    {
        yield return new WaitForSeconds(respawnTimerRemaining);

        // Regrow the tree
        isCutDown = false;
        currentHits = hitsToCut;
        respawnTimerRemaining = 0f;
        isRespawning = false;

        if (treeVisual != null)
            treeVisual.SetActive(true);
        if (treeCollider != null)
            treeCollider.enabled = true;
        if (stumpObject != null)
            stumpObject.SetActive(false);

        HUDManager.Instance?.ShowMessage("A tree has regrown!", 3f);
    }

    // ---- Helper to auto‑find treeVisual if not assigned ----
    private void FindVisualIfMissing()
    {
        if (treeVisual != null) return;

        // Try to find a child with a Renderer or LODGroup (typical for tree visuals)
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            treeVisual = renderer.gameObject;
            Debug.Log($"TreeCuttable: Auto‑assigned treeVisual to {treeVisual.name}");
            return;
        }

        LODGroup lod = GetComponentInChildren<LODGroup>();
        if (lod != null)
        {
            treeVisual = lod.gameObject;
            Debug.Log($"TreeCuttable: Auto‑assigned treeVisual to {treeVisual.name}");
            return;
        }

        // Still null – warn
        Debug.LogWarning("TreeCuttable: Could not auto‑find a visual child. Please assign treeVisual manually.", this);
    }

    // ---- Core interaction methods (unchanged from original) ----
    public void CutTree()
    {
        if (isCutDown || isChopping) return;

        isChopping = true;

        if (AxeAnimatorHelper.Instance != null)
            AxeAnimatorHelper.Instance.PlayChop(chopBoolName, chopAnimationDuration);
        else
            Debug.LogWarning("AxeAnimatorHelper.Instance not found");

        StartCoroutine(ApplyHitAfterDelay());
    }

    private IEnumerator ApplyHitAfterDelay()
    {
        yield return new WaitForSeconds(chopAnimationDuration);
        if (this == null || !gameObject.activeInHierarchy) yield break;
        ApplyHit();
    }

    private void ApplyHit()
    {
        currentHits--;
        HUDManager.Instance?.ShowMessage($"Tree hit! {currentHits} hits left.", 1f);

        PlayCutEffects();
        StartCoroutine(ShakeTree());

        if (currentHits <= 0)
            CutDownTree();
        else
            isChopping = false;
    }

    private void CutDownTree()
    {
        isCutDown = true;
        isChopping = false;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(woodItemName, ItemType.Wood, woodAmount);

        HUDManager.Instance?.ShowMessage($"<color=brown>+{woodAmount} wood</color>", 2f);

        TreeGrassTerrainArea grassArea = GetComponent<TreeGrassTerrainArea>();
        if (grassArea != null)
            grassArea.ClearGrass();

        // Disable the visual LODS object
        if (treeVisual != null)
            treeVisual.SetActive(false);

        // Disable the collider on the Tree parent so player can't interact
        if (treeCollider != null)
            treeCollider.enabled = false;

        // Show stump
        if (stumpObject != null)
            stumpObject.SetActive(true);

        if (respawnAfterDelay)
        {
            respawnTimerRemaining = respawnDelay;
            HUDManager.Instance?.ShowMessage($"Tree will regrow in {respawnDelay / 60f:F1} min.", 3f);
            StartCoroutine(RespawnTreeAfterDelay());
        }
    }

    private IEnumerator RespawnTreeAfterDelay()
    {
        float elapsed = 0f;
        while (elapsed < respawnDelay)
        {
            respawnTimerRemaining = respawnDelay - elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isCutDown = false;
        currentHits = hitsToCut;
        isRespawning = false;

        // Re-enable the visual
        if (treeVisual != null)
            treeVisual.SetActive(true);

        // Re-enable the collider
        if (treeCollider != null)
            treeCollider.enabled = true;

        // Hide stump
        if (stumpObject != null)
            stumpObject.SetActive(false);

        respawnTimerRemaining = 0f;
        HUDManager.Instance?.ShowMessage("A tree has regrown!", 3f);
    }

    private void PlayCutEffects()
    {
        if (cutEffect != null) cutEffect.Play();
        if (cutSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXOneShot(cutSound, 1f);
    }

    private IEnumerator ShakeTree()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }

    public string GetInteractionPrompt()
    {
        if (isCutDown)
            return respawnAfterDelay ? "Tree is regrowing..." : "Stump";
        if (isChopping)
            return "Chopping...";
        return $"Press F to cut tree ({currentHits}/{hitsToCut} hits)";
    }

    public void Highlight(bool active) { }
}