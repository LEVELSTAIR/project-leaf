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
    public GameObject stumpObject;

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
    private Renderer[] treeRenderers;
    private Collider[] treeColliders;

    public bool IsCutDown => isCutDown;
    public int CurrentHits => currentHits;
    public float RespawnTimeRemaining => respawnTimerRemaining;

    // For saving respawn progress
    private float respawnTimerRemaining = 0f;

    private void Start()
    {
        currentHits = hitsToCut;
        treeCollider = GetComponent<Collider>();

        treeRenderers = GetComponentsInChildren<Renderer>();
        treeColliders = GetComponentsInChildren<Collider>();

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

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(woodItemName, ItemType.Wood, woodAmount);

        HUDManager.Instance?.ShowMessage($"<color=brown>+{woodAmount} wood</color>", 2f);

        TreeGrassTerrainArea grassArea = GetComponent<TreeGrassTerrainArea>();
        if (grassArea != null)
            grassArea.ClearGrass();

        // Disable visuals and colliders
        foreach (Renderer rend in treeRenderers)
            rend.enabled = false;
        foreach (Collider col in treeColliders)
            col.enabled = false;

        if (stumpObject != null)
            stumpObject.SetActive(true);

        // Start respawn if enabled
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

        // Re-enable tree
        isCutDown = false;
        currentHits = hitsToCut;

        foreach (Renderer rend in treeRenderers)
            rend.enabled = true;
        foreach (Collider col in treeColliders)
            col.enabled = true;

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

    // ---------- Save / Load ----------

    public TreeState GetSaveState()
    {
        TreeState state = new TreeState();
        TreeIdentifier id = GetComponent<TreeIdentifier>();
        state.id = id != null ? id.UniqueID : "";
        state.isCutDown = isCutDown;
        state.currentHits = currentHits;
        state.respawnTimeRemaining = isCutDown && respawnAfterDelay ? respawnTimerRemaining : 0f;
        return state;
    }

    public void LoadState(TreeState state)
    {
        isCutDown = state.isCutDown;
        currentHits = state.currentHits;

        // Apply visual state immediately
        foreach (Renderer rend in treeRenderers)
            rend.enabled = !isCutDown;
        foreach (Collider col in treeColliders)
            col.enabled = !isCutDown;
        if (stumpObject != null)
            stumpObject.SetActive(isCutDown);

        // Handle respawn timer if cut down and respawn enabled
        if (isCutDown && respawnAfterDelay && state.respawnTimeRemaining > 0f)
        {
            respawnTimerRemaining = state.respawnTimeRemaining;
            StartCoroutine(RespawnAfterDelay(state.respawnTimeRemaining));
        }
        else
        {
            respawnTimerRemaining = 0f;
        }
    }

    private IEnumerator RespawnAfterDelay(float remaining)
    {
        yield return new WaitForSeconds(remaining);
        // Re-enable tree
        isCutDown = false;
        currentHits = hitsToCut;

        foreach (Renderer rend in treeRenderers)
            rend.enabled = true;
        foreach (Collider col in treeColliders)
            col.enabled = true;

        if (stumpObject != null)
            stumpObject.SetActive(false);

        respawnTimerRemaining = 0f;
        HUDManager.Instance?.ShowMessage("A tree has regrown!", 3f);
    }
}