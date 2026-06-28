using UnityEngine;
using System.Collections;
using ProjectLeaf.Interfaces;

public class ClayHarvester : MonoBehaviour, IInteractable
{
    [Header("Harvest Settings")]
    public string resourceName = "Clay Deposit";
    public int hitsToHarvest = 1;                // swings needed to get one batch
    public int amountPerHarvest = 2;
    public int maxHarvests = 3;                  // total batches before depletion

    [Header("Animation")]
    public string swingBoolName = "IsSwinging";
    public float swingDuration = 1.0f;           // length of the swing animation

    [Header("Respawn (optional)")]
    public bool respawnAfterDelay = false;
    public float respawnDelay = 300f;
    public GameObject depositPrefabForRespawn;

    [Header("Visual & Audio")]
    public ParticleSystem harvestEffect;
    public AudioClip harvestSound;
    public GameObject depletedVisual;            // e.g., a greyed-out model

    [Header("Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeAmount = 0.05f;

    private int currentHits = 0;                 // hits left for current harvest
    private int totalHarvestsUsed = 0;
    private bool isHarvesting = false;
    private bool isDepleted = false;
    private AudioSource audioSource;
    private GameObject currentDepletedObject;

    private void Start()
    {
        currentHits = hitsToHarvest;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && harvestSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public string InteractionPrompt
    {
        get
        {
            if (isDepleted) return "Depleted";
            if (isHarvesting) return "Harvesting...";
            if (!IsPlayerHoldingPickaxe())
                return "Need a pickaxe!";
            return $"Harvest {resourceName} ({totalHarvestsUsed + 1}/{maxHarvests})";
        }
    }

    public void Interact()
    {
        // ---- Tool check ----
        if (!IsPlayerHoldingPickaxe())
        {
            NotificationManager.Instance?.ShowNotification("You need a pickaxe to mine clay.");
            return;
        }

        // ---- Depletion check ----
        if (isDepleted || totalHarvestsUsed >= maxHarvests)
        {
            NotificationManager.Instance?.ShowNotification("This deposit is depleted.");
            return;
        }

        // ---- Prevent overlapping ----
        if (isHarvesting) return;

        // ---- Start harvest ----
        isHarvesting = true;

        // Trigger pickaxe animation
        if (PickaxeAnimatorHelper.Instance != null)
            PickaxeAnimatorHelper.Instance.PlaySwing(swingBoolName, swingDuration);
        else
            Debug.LogWarning("PickaxeAnimatorHelper not found – animation will not play");

        // Wait for the swing to finish, then apply the hit
        StartCoroutine(ApplyHitAfterDelay());
    }

    private IEnumerator ApplyHitAfterDelay()
    {
        yield return new WaitForSeconds(swingDuration);
        if (this == null || !gameObject.activeInHierarchy) yield break;
        ApplyHit();
    }

    private void ApplyHit()
    {
        currentHits--;

        PlayHarvestEffects();
        StartCoroutine(ShakeClayDeposit());

        if (currentHits <= 0)
        {
            // Successfully harvested a batch
            totalHarvestsUsed++;
            currentHits = hitsToHarvest;   // reset for next batch

            // Add clay to inventory
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem("Clay", ItemType.Material, amountPerHarvest);
                string msg = $"+{amountPerHarvest} Clay!";
                HUDManager.Instance?.ShowMessage(msg, 2f);
                Debug.Log(msg);
            }

            // Check if deposit is now depleted
            if (totalHarvestsUsed >= maxHarvests)
            {
                DepleteDeposit();
            }
        }

        // Allow next interaction
        isHarvesting = false;
    }

    private void DepleteDeposit()
    {
        isDepleted = true;

        // Visual feedback
        if (depletedVisual != null)
        {
            currentDepletedObject = Instantiate(depletedVisual, transform.position, transform.rotation);
        }
        else
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.gray;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject);

        if (respawnAfterDelay && depositPrefabForRespawn != null)
        {
            StartCoroutine(RespawnDepositAfterDelay(transform.position, transform.rotation));
        }
    }

    private IEnumerator RespawnDepositAfterDelay(Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (currentDepletedObject != null)
            Destroy(currentDepletedObject);

        GameObject newDeposit = Instantiate(depositPrefabForRespawn, pos, rot);
        ClayHarvester newScript = newDeposit.GetComponent<ClayHarvester>();
        if (newScript != null)
        {
            newScript.totalHarvestsUsed = 0;
            newScript.currentHits = newScript.hitsToHarvest;
            newScript.isDepleted = false;
            newScript.isHarvesting = false;
        }

        HUDManager.Instance?.ShowMessage("Clay deposit has regrown!", 3f);
    }

    private void PlayHarvestEffects()
    {
        if (harvestEffect != null) harvestEffect.Play();
        if (harvestSound != null && audioSource != null)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFXOneShot(harvestSound, 1f);
            else
                Debug.LogWarning("SoundManager.Instance not found – cannot play harvest sound.");
        }
    }

    private IEnumerator ShakeClayDeposit()
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

    // ---- Tool check using your WeaponHolderController ----
    private bool IsPlayerHoldingPickaxe()
    {
        if (WeaponHolderController.Instance == null)
            return false;
        return WeaponHolderController.Instance.CurrentTool == "Pickaxe";
    }

    public void Highlight(bool active)
    {
        // Prevents the exception when the object has been destroyed
        if (this == null || gameObject == null) return;

        var highlighter = GetComponent<Highlighter>();
        if (highlighter != null) highlighter.SetHighlight(active);
    }
}