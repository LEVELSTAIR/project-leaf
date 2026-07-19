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

    [Header("Visual & Audio")]
    public ParticleSystem cutEffect;
    public AudioClip cutSound;
    public GameObject stumpObject;          // <-- NEW: assign a child stump GameObject

    [Header("Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeAmount = 0.05f;

    [Header("Axe Animation")]
    public string chopBoolName = "IsChopping";
    public float chopAnimationDuration = 1.0f;

    private bool isCutDown = false;
    private bool isChopping = false;
    private Collider treeCollider;
    private GameObject treeVisual;
    private Renderer[] treeRenderers;       // For toggling visibility
    private Collider[] treeColliders;       // For toggling interaction

    private void Start()
    {
        currentHits = hitsToCut;
        treeCollider = GetComponent<Collider>();

        // Find all renderers and colliders on this GameObject and children
        treeRenderers = GetComponentsInChildren<Renderer>();
        treeColliders = GetComponentsInChildren<Collider>();

        // If stumpObject is a child, make sure it starts hidden
        if (stumpObject != null)
            stumpObject.SetActive(false);
    }

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

        // Add wood
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(woodItemName, ItemType.Wood, woodAmount);

        HUDManager.Instance?.ShowMessage($"<color=brown>+{woodAmount} wood</color>", 2f);

        // Clear grass (if any)
        TreeGrassTerrainArea grassArea = GetComponent<TreeGrassTerrainArea>();
        if (grassArea != null)
            grassArea.ClearGrass();

        // ---- DISABLE instead of Destroy ----
        // Hide all renderers
        foreach (Renderer rend in treeRenderers)
            rend.enabled = false;

        // Disable all colliders (so player can't interact)
        foreach (Collider col in treeColliders)
            col.enabled = false;

        // Show stump (if assigned)
        if (stumpObject != null)
            stumpObject.SetActive(true);

        // Start respawn coroutine if enabled
        if (respawnAfterDelay)
        {
            HUDManager.Instance?.ShowMessage($"Tree will regrow in {respawnDelay / 60f:F1} min.", 3f);
            StartCoroutine(RespawnTreeAfterDelay());
        }
    }

    private IEnumerator RespawnTreeAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Re-enable the tree
        isCutDown = false;
        currentHits = hitsToCut;

        // Show renderers
        foreach (Renderer rend in treeRenderers)
            rend.enabled = true;

        // Re-enable colliders
        foreach (Collider col in treeColliders)
            col.enabled = true;

        // Hide stump
        if (stumpObject != null)
            stumpObject.SetActive(false);

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

    public void Highlight(bool active) { /* optional */ }
}