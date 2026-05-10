using UnityEngine;
using UnityEngine.AI;

public class NPCControllerWithAreaPoints : MonoBehaviour
{
    [Header("Roam Area")]
    [SerializeField] private Collider roamArea;               // Assign a Box / Sphere / Capsule collider
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float destinationThreshold = 1.5f;
    [SerializeField] private float idleTimeAtDestination = 2f; // seconds to pause after arrival

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float fovAngle = 90f;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private Transform eyes;                   // optional raycast origin

    [Header("Fleeing")]
    [SerializeField] private float fleeDistance = 20f;
    [SerializeField] private float runSpeed = 7f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    private NavMeshAgent agent;
    private bool isFleeing = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector3 currentDestination;

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

        if (roamArea == null)
        {
            // Try to get the collider from this GameObject or its children
            roamArea = GetComponent<Collider>();
            if (roamArea == null)
                roamArea = GetComponentInChildren<Collider>();
        }

        if (roamArea == null)
        {
            Debug.LogError("No roam area collider assigned to DeerRoamAI!");
            return;
        }

        // Start wandering
        PickNewDestination();
    }

    private void Update()
    {
        // Update animator
        if (animator)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            animator.SetBool("IsRunning", isFleeing);
        }

        // If outside the roam area (e.g. pushed out), go back inside
        if (!IsPointInsideArea(transform.position))
            PickNewDestination();

        if (isFleeing)
        {
            FleeUpdate();
            return;
        }

        // Detection runs even while waiting
        DetectPlayer();

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                StopWaiting();
                PickNewDestination();
            }
        }
        else
        {
            // Normal roaming
            if (!agent.pathPending && agent.remainingDistance <= destinationThreshold)
            {
                StartWaiting();
            }
        }
    }

    // ----------------------------------------------------------
    //  ROAMING
    // ----------------------------------------------------------
    private void PickNewDestination()
    {
        Vector3 randomPoint = GetRandomPointInsideCollider();
        currentDestination = randomPoint;
        agent.SetDestination(randomPoint);
        agent.isStopped = false;
        agent.speed = wanderSpeed;
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = idleTimeAtDestination;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
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

        // Line-of-sight
        if (obstructionMask != 0)
        {
            Vector3 origin = eyes ? eyes.position : transform.position + Vector3.up * 1f;
            if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, detectionRange, obstructionMask))
                if (hit.transform != player) return;
        }

        // Player seen – flee!
        StopWaiting();
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
        Vector3 desired = transform.position + fleeDir * fleeDistance;

        // Constrain flee target to roam area
        Vector3 clamped = ClampPointToArea(desired);

        // Find a valid NavMesh point near the clamped position
        if (NavMesh.SamplePosition(clamped, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            currentDestination = hit.position;
        else
            currentDestination = ClampPointToArea(transform.position + fleeDir * fleeDistance * 0.5f);

        agent.SetDestination(currentDestination);
    }

    private void FleeUpdate()
    {
        if (!agent.pathPending && agent.remainingDistance <= destinationThreshold)
        {
            StopFleeing();
        }
    }

    private void StopFleeing()
    {
        isFleeing = false;
        agent.speed = wanderSpeed;

        // Resume roaming from current location
        PickNewDestination();
    }

    // ----------------------------------------------------------
    //  AREA TOOLS
    // ----------------------------------------------------------
    private Vector3 GetRandomPointInsideCollider()
    {
        // Try several times to find a valid NavMesh point inside the collider
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = GetRandomPointInColliderVolume(roamArea);
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                // Extra check: ensure the sampled point is still inside the collider
                if (IsPointInsideArea(hit.position))
                    return hit.position;
            }
        }
        // Fallback: just return the center of the collider
        return roamArea.bounds.center;
    }

    private bool IsPointInsideArea(Vector3 point)
    {
        // For a convex collider, a point is inside if the distance to its surface is > 0
        Vector3 closest = roamArea.ClosestPoint(point);
        return Vector3.Distance(point, closest) < 0.01f;
    }

    private Vector3 ClampPointToArea(Vector3 point)
    {
        // ClosestPoint returns a surface point if the original is outside,
        // or a point on the surface if inside? Actually it returns the point itself if inside.
        // So we use it to project external points onto the boundary.
        return roamArea.ClosestPoint(point);
    }

    private Vector3 GetRandomPointInColliderVolume(Collider col)
    {
        if (col is BoxCollider box)
        {
            Vector3 localCenter = box.center;
            Vector3 halfExtents = box.size * 0.5f;
            Vector3 randomLocal = new Vector3(
                Random.Range(-halfExtents.x, halfExtents.x),
                Random.Range(-halfExtents.y, halfExtents.y),
                Random.Range(-halfExtents.z, halfExtents.z)
            );
            return box.transform.TransformPoint(localCenter + randomLocal);
        }
        else if (col is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x,
                                                       sphere.transform.lossyScale.y,
                                                       sphere.transform.lossyScale.z);
            Vector3 randomDir = Random.insideUnitSphere;
            return center + randomDir * Random.Range(0f, radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Simple approximation: random point inside the bounding sphere of the capsule
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            float radius = capsule.radius;
            float height = capsule.height * 0.5f;
            // Random point along the axis
            float randomHeight = Random.Range(-height, height);
            Vector3 axis = capsule.transform.up; // capsule.mainAxis is up by default
            if (capsule.direction == 0) axis = capsule.transform.right;
            else if (capsule.direction == 2) axis = capsule.transform.forward;

            Vector3 pointOnAxis = center + axis * randomHeight;
            Vector3 randomDir = Random.insideUnitSphere;
            return pointOnAxis + randomDir * Random.Range(0f, radius);
        }
        // Fallback for other collider types
        return col.bounds.center;
    }

    // ----------------------------------------------------------
    //  VISUALISATION
    // ----------------------------------------------------------
    private void OnDrawGizmos()
    {
        // Draw roam area (wire cube for box, wire sphere for sphere, etc.)
        if (roamArea != null)
        {
            Gizmos.color = Color.cyan;
            if (roamArea is BoxCollider box)
            {
                Matrix4x4 originalMatrix = Gizmos.matrix;
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = originalMatrix;
            }
            else if (roamArea is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x,
                                                           sphere.transform.lossyScale.y,
                                                           sphere.transform.lossyScale.z);
                Gizmos.DrawWireSphere(center, radius);
            }
            else if (roamArea is CapsuleCollider capsule)
            {
                // Draw approximate capsule as wire sphere? Better to just draw the bounds
                Gizmos.DrawWireCube(roamArea.bounds.center, roamArea.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(roamArea.bounds.center, roamArea.bounds.size);
            }
        }

        // NavMesh path (green)
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
            Vector3 left  = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0,  fovAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, left * detectionRange);
            Gizmos.DrawRay(transform.position, right * detectionRange);
        }
    }
}
