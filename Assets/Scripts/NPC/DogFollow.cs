using UnityEngine;
using UnityEngine.AI;

public class DogFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Following")]
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float updatePathRate = 0.2f;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 7f;
    [Tooltip("Player velocity magnitude above this is considered running")]
    [SerializeField] private float runSpeedThreshold = 5.5f;   // between your walk & sprint speed

    [Header("References")]
    [SerializeField] private Animator animator;

    // References
    private NavMeshAgent agent;
    private Rigidbody playerRb;
    private float nextUpdateTime;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("DogFollow requires a NavMeshAgent on the dog.");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        agent.stoppingDistance = stopDistance;

        // Find the player
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Cache the player's Rigidbody to read velocity
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (player == null) return;

        // ----- Dynamic speed based on player movement -----
        if (playerRb != null)
        {
            float playerSpeed = playerRb.linearVelocity.magnitude;
            // Ignore vertical speed (jumping) by using horizontal magnitude only
            Vector3 horizontalVelocity = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;

            if (horizontalSpeed > runSpeedThreshold)
            {
                agent.speed = runSpeed;
                if (animator) animator.SetBool("IsRunning", true);
            }
            else
            {
                agent.speed = walkSpeed;
                if (animator) animator.SetBool("IsRunning", false);
            }
        }

        // ----- Update animation speed -----
        if (animator != null)
            animator.SetFloat("Speed", agent.velocity.magnitude);

        // ----- Pathfinding update (periodic) -----
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updatePathRate;

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance > followDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else if (distance <= stopDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;   // immediately zero velocity for animation
            }
            else
            {
                // Hysteresis: resume if we drifted a bit out of stop range
                if (agent.isStopped && distance > stopDistance + 0.3f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
            }
        }

        // ----- Smooth rotation -----
        if (!agent.isStopped && agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 200f * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }
}
