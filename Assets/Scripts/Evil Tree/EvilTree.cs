using UnityEngine;

public class EvilTree : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 3f;

    [Header("Field of View")]
    [SerializeField] private float fovAngle = 90f;
    [SerializeField] private bool enableLineOfSight = true;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Hand Colliders (solid, not triggers)")]
    [SerializeField] private Collider[] handColliders;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject attackEffect;

    [Header("Capture")]
    [SerializeField] private GameObject capturedEffect;
    [SerializeField] private Color capturedColor = Color.gray;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip detectionSound;   // played when first detecting the player
    [SerializeField] private AudioClip attackSound;      // played when starting an attack

    private int currentHealth;
    private bool isPlayerDetected = false;
    private bool isAttacking = false;
    private bool isCaptured = false;
    private bool wasAttacking = false;
    private bool hasHitThisSwing = false;    // prevents multiple hits per swing

    private bool wasDetectedPreviously = false; // to trigger detection sound only once

    private Renderer treeRenderer;
    private Material originalMaterial;
    private Color originalColor;

    private void Start()
    {
        currentHealth = maxHealth;

        // Ensure Rigidbody on root (kinematic)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            Debug.Log("EvilTree: Added Kinematic Rigidbody to root.");
        }
        else
        {
            rb.isKinematic = true;
        }

        // Set hand colliders to solid, not triggers, and match root layer
        int rootLayer = gameObject.layer;
        foreach (Collider col in handColliders)
        {
            if (col != null)
            {
                col.gameObject.layer = rootLayer;
                col.isTrigger = false;
                col.enabled = true;
                Debug.Log($"Hand collider {col.name} set to layer {LayerMask.LayerToName(rootLayer)}, solid and enabled.");
            }
        }

        // Find player if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogWarning("EvilTree: No player found!");
        }

        // Ensure player has a non-kinematic Rigidbody
        if (player != null)
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("EvilTree: Player has no Rigidbody! Adding one (non-kinematic).");
                playerRb = player.gameObject.AddComponent<Rigidbody>();
                playerRb.isKinematic = false;
            }
            else if (playerRb.isKinematic)
            {
                Debug.LogWarning("EvilTree: Player rigidbody is kinematic. Setting to non-kinematic for collision detection.");
                playerRb.isKinematic = false;
            }

            // Check layer collision
            int playerLayer = player.gameObject.layer;
            bool canCollide = !Physics.GetIgnoreLayerCollision(rootLayer, playerLayer);
            Debug.Log($"EvilTree: Layers can collide: {LayerMask.LayerToName(rootLayer)} + {LayerMask.LayerToName(playerLayer)} = {canCollide}");
        }

        if (animator == null) animator = GetComponent<Animator>();

        treeRenderer = GetComponent<Renderer>();
        if (treeRenderer != null)
        {
            originalMaterial = treeRenderer.material;
            originalColor = originalMaterial.color;
        }

        wasDetectedPreviously = false;
    }

    private void Update()
    {
        if (isCaptured) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ---- Detection ----
        isPlayerDetected = false;
        if (distance <= detectionRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            if (angleToPlayer <= fovAngle * 0.5f)
            {
                if (enableLineOfSight)
                {
                    Vector3 origin = transform.position + Vector3.up * 1f;
                    Vector3 target = player.position + Vector3.up * 1f;
                    RaycastHit hit;
                    if (Physics.Linecast(origin, target, out hit, obstacleMask))
                    {
                        if (hit.transform == player)
                            isPlayerDetected = true;
                    }
                }
                else
                {
                    isPlayerDetected = true;
                }
            }
        }

        // ---- Play detection sound when first detected ----
        if (isPlayerDetected && !wasDetectedPreviously)
        {
            PlaySound(detectionSound);
            wasDetectedPreviously = true;
        }
        else if (!isPlayerDetected)
        {
            wasDetectedPreviously = false; // reset so we can play again later
        }

        // ---- Attack state ----
        bool inAttackRange = distance <= attackRange;
        isAttacking = isPlayerDetected && inAttackRange;

        // Reset hit flag when attack starts
        if (isAttacking && !wasAttacking)
        {
            hasHitThisSwing = false;
            if (animator != null)
                animator.SetTrigger("Attack");

            // ---- Play attack sound ----
            PlaySound(attackSound);

            if (attackEffect != null)
            {
                GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
            Debug.Log("EvilTree: Attack triggered!");
        }

        // Update animator
        if (animator != null)
        {
            animator.SetBool("PlayerDetected", isPlayerDetected);
            animator.SetBool("IsAttacking", isAttacking);
        }

        wasAttacking = isAttacking;

        // ---- MANUAL OVERLAP CHECK (FALLBACK) ----
        if (isAttacking && !hasHitThisSwing)
        {
            CheckManualOverlap();
        }
    }

    private void CheckManualOverlap()
    {
        if (player == null) return;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null) return;

        foreach (Collider hand in handColliders)
        {
            if (hand == null) continue;

            // Use bounds intersection (fast but approximate)
            if (hand.bounds.Intersects(playerCollider.bounds))
            {
                Debug.Log($"MANUAL OVERLAP DETECTED: {hand.name} intersects player!");
                ApplyDamageToPlayer();
                hasHitThisSwing = true; // prevent multiple hits per swing
                break;
            }
        }
    }

    private void ApplyDamageToPlayer()
    {
        if (isCaptured) return;

        PlayerHealthManager playerHealth = player.GetComponent<PlayerHealthManager>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} punched player for {attackDamage} damage (manual overlap)!");
        }
        else
        {
            Debug.LogWarning("EvilTree: Player has no PlayerHealthManager!");
        }
    }

    // Called by HandColliderForwarder (keep for future use)
    public void OnHandCollisionEnter(Collision collision)
    {
        // This is kept but will likely not be called if physics fails.
        Debug.Log($"EvilTree.OnHandCollisionEnter: collision with {collision.gameObject.name}");
        if (isCaptured) return;
        if (!isAttacking) return;
        if (collision.transform != player) return;

        ApplyDamageToPlayer();
    }

    public void TakeDamage(int damage)
    {
        if (isCaptured) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
        else
        {
            if (animator != null)
                animator.SetTrigger("Hurt");
        }
    }

    private void Die()
    {
        if (animator != null)
            animator.SetTrigger("Die");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1f);
    }

    public void Capture()
    {
        if (isCaptured) return;
        isCaptured = true;
        isAttacking = false;
        isPlayerDetected = false;

        Collider mainCol = GetComponent<Collider>();
        if (mainCol != null) mainCol.enabled = true;

        if (capturedEffect != null)
            Instantiate(capturedEffect, transform.position, Quaternion.identity);

        if (treeRenderer != null)
            treeRenderer.material.color = capturedColor;

        if (animator != null)
        {
            animator.SetTrigger("Captured");
            animator.SetBool("IsCaptured", true);
        }

        Debug.Log($"{gameObject.name} has been captured!");
    }

    public bool IsCaptured() => isCaptured;

    public void Uncapture()
    {
        if (!isCaptured) return;
        isCaptured = false;

        Collider mainCol = GetComponent<Collider>();
        if (mainCol != null) mainCol.enabled = true;

        if (treeRenderer != null)
            treeRenderer.material.color = originalColor;

        if (animator != null)
            animator.SetBool("IsCaptured", false);
    }

    // ---------- Sound Helper ----------
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXOneShot(clip);
        else
            Debug.LogWarning("SoundManager.Instance not found – sound not played.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward;
        float halfFOV = fovAngle * 0.5f;
        Vector3 leftBoundary = Quaternion.Euler(0, -halfFOV, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, halfFOV, 0) * forward;
        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}