using UnityEngine;

public class EvilTree : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Field of View")]
    [SerializeField] private float fovAngle = 90f;           // degrees (total angle)
    [SerializeField] private bool enableLineOfSight = true;  // check for obstacles
    [SerializeField] private LayerMask obstacleMask;         // what blocks sight

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject attackEffect;

    [Header("Capture")]
    [SerializeField] private GameObject capturedEffect;      // optional particle when captured
    [SerializeField] private Color capturedColor = Color.gray; // optional material tint

    private int currentHealth;
    private float attackTimer = 0f;
    private bool isPlayerInRange = false;
    private bool isCaptured = false;
    private Renderer treeRenderer;
    private Material originalMaterial;
    private Color originalColor;

    private void Start()
    {
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogWarning("EvilTree: No player found!");
        }

        if (animator == null) animator = GetComponent<Animator>();

        // Cache renderer for visual effects
        treeRenderer = GetComponent<Renderer>();
        if (treeRenderer != null)
        {
            originalMaterial = treeRenderer.material;
            originalColor = originalMaterial.color;
        }
    }

    private void Update()
    {
        // If captured, do nothing (tree is neutralised)
        if (isCaptured) return;

        if (player == null) return;

        // Update cooldown
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        // Check if player is in detection range
        float distance = Vector3.Distance(transform.position, player.position);
        bool playerDetected = false;

        if (distance <= detectionRange)
        {
            // Check FOV angle
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

            if (angleToPlayer <= fovAngle * 0.5f)
            {
                // Optional line-of-sight check
                if (enableLineOfSight)
                {
                    Vector3 origin = transform.position + Vector3.up * 1f; // eye height
                    Vector3 target = player.position + Vector3.up * 1f;
                    RaycastHit hit;
                    if (Physics.Linecast(origin, target, out hit, obstacleMask))
                    {
                        if (hit.transform == player)
                            playerDetected = true;
                    }
                }
                else
                {
                    playerDetected = true;
                }
            }
        }

        // Check attack range (no FOV needed for attack, tree can attack if player is close enough)
        isPlayerInRange = distance <= attackRange;

        // Update animator (optional)
        if (animator != null)
        {
            animator.SetBool("PlayerDetected", playerDetected);
            animator.SetBool("IsAttacking", isPlayerInRange && attackTimer <= 0f);
        }

        // Attack if in range and cooldown ready
        if (isPlayerInRange && attackTimer <= 0f)
        {
            Attack();
        }
    }

    private void Attack()
    {
        attackTimer = attackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        if (player != null)
        {
            PlayerHealthManager playerHealth = player.GetComponent<PlayerHealthManager>();
            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage);
            else
                Debug.LogWarning("EvilTree: Player has no Health component!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isCaptured) return; // captured trees cannot be damaged (or you can allow, up to you)

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

    /// <summary>
    /// Called by CageTrap to neutralise the tree.
    /// </summary>
    public void Capture()
    {
        if (isCaptured) return;
        isCaptured = true;

        // Stop any ongoing attack
        attackTimer = 0f;

        // Disable the NavMeshAgent or movement if any (not present in this script, but safe)
        // Disable collider so player can walk through
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // Play capture effect
        if (capturedEffect != null)
        {
            Instantiate(capturedEffect, transform.position, Quaternion.identity);
        }

        // Change visual appearance (material tint)
        if (treeRenderer != null)
        {
            treeRenderer.material.color = capturedColor;
        }

        // Optional: play a sound
        // AudioSource.PlayClipAtPoint(captureSound, transform.position);

        // Optional: trigger an animator state
        if (animator != null)
        {
            animator.SetTrigger("Captured");
            animator.SetBool("IsCaptured", true);
        }

        Debug.Log($"{gameObject.name} has been captured!");
    }

    // Send the captured status
    public bool IsCaptured()
    {
        return isCaptured;
    }

    // Optional: reset capture (if needed for respawning)
    public void Uncapture()
    {
        if (!isCaptured) return;
        isCaptured = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (treeRenderer != null)
        {
            treeRenderer.material.color = originalColor;
        }

        if (animator != null)
        {
            animator.SetBool("IsCaptured", false);
        }
    }

    // Visualize ranges and FOV in editor
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV cone (if we have a forward direction)
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward;
        float halfFOV = fovAngle * 0.5f;
        Vector3 leftBoundary = Quaternion.Euler(0, -halfFOV, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, halfFOV, 0) * forward;
        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}