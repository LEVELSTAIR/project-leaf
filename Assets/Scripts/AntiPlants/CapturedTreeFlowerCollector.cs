using UnityEngine;

/// <summary>
/// Attach to an EvilTree. After capture, the player can collect flower seeds periodically.
/// Seeds regrow after a refresh timer.
/// </summary>
[RequireComponent(typeof(EvilTree))]
public class CapturedTreeFlowerCollector : MonoBehaviour, IInteractable
{
    [Header("Flower Seed Rewards")]
    public FlowerSeedYield[] flowerSeedYields;

    [Header("Collection & Refresh")]
    [Tooltip("Time in seconds after collection until seeds become available again.")]
    public float refreshTime = 300f; // 5 minutes default

    [Tooltip("If true, the tree is destroyed after a certain number of collections.")]
    public bool destroyAfterMaxCollections = false;
    public int maxCollections = 3;

    [Header("Visual Feedback")]
    public ParticleSystem collectionEffect;
    public AudioClip collectionSound;
    public ParticleSystem regrowEffect; // optional effect when refresh completes

    private EvilTree evilTree;
    private bool isCaptured = false;
    private int collectionCount = 0;
    private float lastCollectionTime = -999f;
    private AudioSource audioSource;

    public string InteractionPrompt
    {
        get
        {
            if (!isCaptured) return "Tree is still hostile – capture it first!";
            if (destroyAfterMaxCollections && collectionCount >= maxCollections)
                return "Tree has no more seeds.";

            float timeSinceLast = Time.time - lastCollectionTime;
            if (timeSinceLast < refreshTime)
            {
                int remaining = Mathf.CeilToInt(refreshTime - timeSinceLast);
                return $"Seeds regrowing... Ready in {remaining}s";
            }

            return $"Press F to collect flower seeds ({collectionCount + 1}/{maxCollections})";
        }
    }

    private void Start()
    {
        evilTree = GetComponent<EvilTree>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectionSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (!isCaptured && evilTree != null && evilTree.IsCaptured())
        {
            OnTreeCaptured();
        }

        // Optional: trigger regrow effect exactly when refresh finishes
        if (isCaptured && !destroyAfterMaxCollections || collectionCount < maxCollections)
        {
            float timeSinceLast = Time.time - lastCollectionTime;
            if (timeSinceLast >= refreshTime && timeSinceLast - Time.deltaTime < refreshTime)
            {
                if (regrowEffect != null)
                    regrowEffect.Play();
            }
        }
    }

    private void OnTreeCaptured()
    {
        isCaptured = true;
        lastCollectionTime = -refreshTime; // make it immediately collectable
        Debug.Log($"{gameObject.name} captured – ready for seed collection.");
    }

    public void Interact()
    {
        if (!isCaptured)
        {
            HUDManager.Instance?.ShowMessage("Defeat the tree first!", 2f);
            return;
        }

        if (destroyAfterMaxCollections && collectionCount >= maxCollections)
        {
            HUDManager.Instance?.ShowMessage("This tree has no more seeds.", 2f);
            return;
        }

        float timeSinceLast = Time.time - lastCollectionTime;
        if (timeSinceLast < refreshTime)
        {
            int remaining = Mathf.CeilToInt(refreshTime - timeSinceLast);
            HUDManager.Instance?.ShowMessage($"Seeds regrowing... {remaining}s remaining", 2f);
            return;
        }

        CollectFlowerSeeds();
    }

    private void CollectFlowerSeeds()
    {
        if (flowerSeedYields == null || flowerSeedYields.Length == 0)
        {
            Debug.LogWarning($"No flower seed yields on {gameObject.name}");
            return;
        }

        FlowerSeedYield selected = flowerSeedYields[Random.Range(0, flowerSeedYields.Length)];
        if (selected.flowerSeedData == null)
        {
            Debug.LogError($"Missing FlowerSeedData in yield entry on {gameObject.name}");
            return;
        }

        int amount = Random.Range(selected.minYield, selected.maxYield + 1);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(selected.flowerSeedData.seedName, ItemType.FlowerSeeds, amount);
            string msg = $"<color=magenta>Collected {amount} {selected.flowerSeedData.seedName}(s)!</color>";
            HUDManager.Instance?.ShowMessage(msg, 2f);
            Debug.Log(msg);
        }
        else
        {
            Debug.LogError("InventoryManager missing!");
        }

        collectionCount++;
        lastCollectionTime = Time.time;

        // Effects
        if (collectionEffect != null) collectionEffect.Play();
        if (collectionSound != null && audioSource != null) audioSource.PlayOneShot(collectionSound);

        // Check for destruction after max collections
        if (destroyAfterMaxCollections && collectionCount >= maxCollections)
        {
            Debug.Log($"{gameObject.name} reached max collections – destroying.");
            Destroy(gameObject, 0.5f);
        }
    }

    public void Highlight(bool active) { /* optional highlight */ }
}
