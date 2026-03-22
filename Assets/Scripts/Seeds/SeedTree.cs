using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class SeedTree : MonoBehaviour, IInteractable
{
    [Header("Tree Settings")]
    [SerializeField] private string treeName = "Oak Tree";
    [SerializeField] private string seedType = "Acorn";
    [SerializeField] private int seedAmount = 3;
    [Header("Seed Data (optional)")]
    [SerializeField] private SeedData seedData;
    [SerializeField] private float interactionCooldown = 5f; // Time before tree can be interacted again

    [Header("Reset Timer")]
    [SerializeField] private float resetTime = 60f; // Time for seeds to regrow
    [SerializeField] private bool showTimerInPrompt = true;

    [Header("Visual Effects")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private ParticleSystem seedCollectEffect;
    [SerializeField] private AudioClip seedCollectSound;

    private Renderer treeRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;
    private bool isDepleted = false;
    private float depletionTime;
    private float lastInteractionTime;
    private bool canInteract = true;

    public string InteractionPrompt
    {
        get
        {
            if (!canInteract)
            {
                float timeRemaining = interactionCooldown - (Time.time - lastInteractionTime);
                if (timeRemaining > 0)
                {
                    return $"Tree is shaking... Wait {Mathf.CeilToInt(timeRemaining)}s";
                }
            }

            if (isDepleted)
            {
                if (showTimerInPrompt)
                {
                    float timeRemaining = resetTime - (Time.time - depletionTime);
                    if (timeRemaining > 0)
                    {
                        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                        return $"No seeds left. Regrows in {minutes:00}:{seconds:00}";
                    }
                }
                return "No seeds left. Tree is regrowing...";
            }

            // PlayerInteraction now uses F for collect
            return $"Press F to collect {seedAmount} {seedType}(s) from {treeName}";
        }
    }

    private void Start()
    {
        treeRenderer = GetComponent<Renderer>();

        if (treeRenderer != null)
        {
            originalMaterial = treeRenderer.material;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && seedCollectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        isDepleted = false;
        canInteract = true;
    }

    private void Update()
    {
        // Check if cooldown is over
        if (!canInteract && Time.time >= lastInteractionTime + interactionCooldown)
        {
            canInteract = true;
        }

        // Check if tree should reset
        if (isDepleted && Time.time >= depletionTime + resetTime)
        {
            ResetTree();
        }
    }

    public void Highlight(bool active)
    {
        if (treeRenderer == null) return;

        if (active && !isDepleted && canInteract && highlightMaterial != null)
        {
            treeRenderer.material = highlightMaterial;
        }
        else
        {
            if (originalMaterial != null)
                treeRenderer.material = originalMaterial;
        }
    }

    public void Interact()
    {
        // Check if tree is depleted
        if (isDepleted)
        {
            float timeRemaining = resetTime - (Time.time - depletionTime);
            if (timeRemaining > 0)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                string message = $"No seeds left. Regrows in {minutes:00}:{seconds:00}";
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.ShowMessage(message, 2f);
                }
            }
            return;
        }

        // Check cooldown
        if (!canInteract)
        {
            float timeRemaining = interactionCooldown - (Time.time - lastInteractionTime);
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage($"Tree is shaking! Wait {Mathf.CeilToInt(timeRemaining)}s", 1.5f);
            }
            return;
        }

        // Collect seeds
        CollectSeeds();

        // Start cooldown
        canInteract = false;
        lastInteractionTime = Time.time;

        // Mark as depleted (no more seeds)
        isDepleted = true;
        depletionTime = Time.time;

        // Play effects
        PlayCollectEffects();

        // Shake tree animation
        StartCoroutine(ShakeTree());

        // Visual feedback
        StartCoroutine(FlashGreen());
    }

    private void CollectSeeds()
    {
        // Use SeedData if assigned, otherwise fallback to seedType/seedAmount
        string nameToAdd = seedData != null ? seedData.seedName : seedType;
        int amountToAdd = seedData != null ? seedData.harvestYield : seedAmount;

        if (SeedManager.Instance != null)
        {
            SeedManager.Instance.AddSeeds(nameToAdd, amountToAdd);

            string message = $"<color=green>Collected {amountToAdd} {nameToAdd}(s)!</color>";
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(message, 2f);
            }
            Debug.Log(message);
            return;
        }

        // Fallback to InventoryManager if SeedManager isn't available
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(nameToAdd, ItemType.Seed, amountToAdd);

            string message = $"<color=red>Collected {amountToAdd} {nameToAdd}(s)!</color>";
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowMessage(message, 2f);
            }
            Debug.Log(message);
        }
    }

    private void PlayCollectEffects()
    {
        // Play particle effect
        if (seedCollectEffect != null)
        {
            seedCollectEffect.Play();
        }

        // Play sound
        if (seedCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(seedCollectSound);
        }
    }

    private IEnumerator ShakeTree()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float shakeAmount = 0.05f;

        while (elapsed < 0.3f)
        {
            transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }

    private IEnumerator FlashGreen()
    {
        if (treeRenderer != null)
        {
            treeRenderer.material.color = Color.green;
            yield return new WaitForSeconds(0.2f);

            if (isDepleted)
            {
                // Make tree slightly darker to show it's depleted
                treeRenderer.material.color = new Color(0.4f, 0.3f, 0.2f);
            }
            else if (originalMaterial != null)
            {
                treeRenderer.material = originalMaterial;
            }
        }
    }

    private void ResetTree()
    {
        isDepleted = false;
        canInteract = true;

        // Reset visual
        if (treeRenderer != null && originalMaterial != null)
        {
            treeRenderer.material = originalMaterial;
            treeRenderer.material.color = Color.white;
        }

        string message = $"{treeName} has grown new seeds!";
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowMessage(message, 3f);
        }

        Debug.Log(message);
    }

    // Optional: Show reset time in OnDrawGizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (isDepleted)
        {
            Gizmos.color = Color.red;
            float timeRemaining = resetTime - (Time.time - depletionTime);
            if (timeRemaining > 0)
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
    }
}