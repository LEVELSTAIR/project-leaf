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
    public GameObject treePrefabForRespawn;

    [Header("Visual & Audio")]
    public ParticleSystem cutEffect;
    public AudioClip cutSound;
    public GameObject stumpPrefab;

    [Header("Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeAmount = 0.05f;

    [Header("Axe Animation")]
    public string chopBoolName = "IsChopping";
    public float chopAnimationDuration = 1.0f;

    private bool isCutDown = false;
    private bool isChopping = false;
    private AudioSource audioSource;
    private Collider treeCollider;
    private GameObject treeVisual;
    private GameObject currentStump;

    private void Start()
    {
        currentHits = hitsToCut;
        treeCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && cutSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        var renderer = GetComponentInChildren<Renderer>();
        treeVisual = renderer != null ? renderer.gameObject : gameObject;
    }

    public void CutTree()
    {
        if (isCutDown || isChopping) return;

        isChopping = true;

        if (AxeAnimatorHelper.Instance != null)
        {
            AxeAnimatorHelper.Instance.PlayChop(chopBoolName, chopAnimationDuration);
        }
        else
        {
            Debug.LogWarning("AxeAnimatorHelper.Instance not found – axe animation will not play");
        }

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

        // Clear grass in the oxygen area
        TreeGrassTerrainArea grassArea = GetComponent<TreeGrassTerrainArea>();
        if (grassArea != null)
            grassArea.ClearGrass();

        // Disable all trigger colliders (backup)
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
            if (col.isTrigger) col.enabled = false;

        Vector3 treePos = transform.position;
        Quaternion treeRot = transform.rotation;

        if (stumpPrefab != null)
            currentStump = Instantiate(stumpPrefab, treePos, treeRot);

        Destroy(gameObject);

        if (respawnAfterDelay && treePrefabForRespawn != null)
        {
            HUDManager.Instance?.ShowMessage($"Tree will regrow in {respawnDelay / 60f:F1} min.", 3f);
            StartCoroutine(RespawnTreeAfterDelay(treePos, treeRot));
        }
    }

    private IEnumerator RespawnTreeAfterDelay(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (currentStump != null)
            Destroy(currentStump);

        GameObject newTree = Instantiate(treePrefabForRespawn, position, rotation);
        TreeCuttable newScript = newTree.GetComponent<TreeCuttable>();
        if (newScript != null)
        {
            newScript.currentHits = newScript.hitsToCut;
            newScript.isCutDown = false;
            newScript.isChopping = false;
        }

        HUDManager.Instance?.ShowMessage("A tree has regrown!", 3f);
    }

    private void PlayCutEffects()
    {
        if (cutEffect != null) cutEffect.Play();
        if (cutSound != null && audioSource != null)
            audioSource.PlayOneShot(cutSound);
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