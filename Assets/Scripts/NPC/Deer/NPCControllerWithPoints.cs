using UnityEngine;
using UnityEngine.AI;

public class NPCControllerWithPoints : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float waypointThreshold = 1.5f;
    [SerializeField] private float idleTimeAtWaypoint = 2f;   // seconds to wait at each point

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float fovAngle = 90f;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private Transform eyes;

    [Header("Fleeing")]
    [SerializeField] private float fleeDistance = 20f;
    [SerializeField] private float runSpeed = 7f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isFleeing = false;
    private Vector3 fleeTarget;

    private bool isWaiting = false;          // currently idle at a waypoint?
    private float waitTimer = 0f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("NavMeshAgent missing on " + gameObject.name);

        if (animator == null)
            animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Start moving to the first waypoint
        if (waypoints.Length > 0)
            GoToCurrentWaypoint();
    }

    private void Update()
    {
        // Always feed the Animator
        if (animator)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            Debug.Log($"Deer speed: {speed}");
            animator.SetBool("IsRunning", isFleeing);
        }

        if (isFleeing)
        {
            FleeUpdate();
            return;
        }

        // Even while waiting, keep looking for the player
        DetectPlayer();

        if (isWaiting)
        {
            // Wait at the waypoint
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                StopWaiting();
                AdvanceWaypoint();
                GoToCurrentWaypoint();
            }
        }
        else
        {
            // Normal patrol movement
            PatrolUpdate();
        }
    }

    // ----------------------------------------------------------
    //  PATROL
    // ----------------------------------------------------------
    private void PatrolUpdate()
    {
        if (waypoints.Length == 0) return;

        // Are we close enough to the current waypoint?
        if (!agent.pathPending && agent.remainingDistance <= waypointThreshold)
        {
            StartWaiting();   // begin idle period at this point
        }
    }

    private void AdvanceWaypoint()
    {
        if (waypoints.Length == 1)
            return; // stay on the same single point
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void GoToCurrentWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        agent.isStopped = false;
        agent.speed = walkSpeed;
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = idleTimeAtWaypoint;
        agent.isStopped = true;           // stop moving
        agent.velocity = Vector3.zero;    // clear any residual velocity
    }

    private void StopWaiting()
    {
        isWaiting = false;
        waitTimer = 0f;
        agent.isStopped = false;
    }

    // ----------------------------------------------------------
    //  DETECTION
    // ----------------------------------------------------------
    private void DetectPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) > fovAngle * 0.5f) return;

        // Line-of-sight check
        if (obstructionMask != 0)
        {
            Vector3 rayOrigin = eyes ? eyes.position : transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, dirToPlayer, out RaycastHit hit, detectionRange, obstructionMask))
            {
                if (hit.transform != player) return;
            }
        }

        // Player seen – stop whatever we're doing and flee
        StopWaiting();          // cancel any idle
        StartFleeing();
    }

    // ----------------------------------------------------------
    //  FLEEING
    // ----------------------------------------------------------
    private void StartFleeing()
    {
        isFleeing = true;
        agent.isStopped = false;
        agent.speed = runSpeed;

        Vector3 fleeDir = (transform.position - player.position).normalized;
        Vector3 target = transform.position + fleeDir * fleeDistance;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            fleeTarget = hit.position;
        else
            fleeTarget = transform.position + fleeDir * fleeDistance * 0.5f;

        agent.SetDestination(fleeTarget);
    }

    private void FleeUpdate()
    {
        if (!agent.pathPending && agent.remainingDistance <= waypointThreshold)
            StopFleeing();
    }

    private void StopFleeing()
    {
        isFleeing = false;
        agent.speed = walkSpeed;

        // Return to the nearest patrol waypoint
        ReturnToPatrol();
    }

    private void ReturnToPatrol()
    {
        if (waypoints.Length == 0) return;

        // Find the closest waypoint
        int closest = 0;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < minDist)
            {
                minDist = d;
                closest = i;
            }
        }

        currentWaypointIndex = closest;
        StopWaiting();   // ensure waiting state is cleared
        GoToCurrentWaypoint();
    }

    // ----------------------------------------------------------
    //  VISUALISATION
    // ----------------------------------------------------------
    private void OnDrawGizmos()
    {
        // Patrol path (cyan)
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }

        // Current NavMesh path (green)
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range & FOV
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (fovAngle > 0)
        {
            Gizmos.color = Color.red;
            Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0, fovAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, left * detectionRange);
            Gizmos.DrawRay(transform.position, right * detectionRange);
        }
    }
}