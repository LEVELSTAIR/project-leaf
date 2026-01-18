using UnityEngine;
using UnityEngine.AI;

public class AntiPlantController : MonoBehaviour
{
    public enum State { Passive, Aggressive, Tamed }

    [Header("Settings")]
    public State currentState = State.Aggressive;
    public float detectRadius = 10f;
    public float moveSpeed = 3.5f;

    [Header("References")]
    public NavMeshAgent agent;
    public GameObject hostileVisual;
    public GameObject tamedVisual;

    private Transform playerTarget;

    private void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        UpdateVisuals();
    }

    private void Update()
    {
        if (currentState == State.Tamed) return;

        // Simple Player Detection
        if (playerTarget == null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, LayerMask.GetMask("Player"));
            if (hits.Length > 0)
            {
                playerTarget = hits[0].transform;
            }
        }
        else
        {
            // Chase Player
            agent.SetDestination(playerTarget.position);
        }
    }

    public void Tame()
    {
        currentState = State.Tamed;
        agent.isStopped = true;
        // agent.enabled = false; // Optional logic
        UpdateVisuals();
        NotificationManager.Instance?.ShowNotification("Anti-Plant Tamed!");
    }

    private void UpdateVisuals()
    {
        if (hostileVisual != null) hostileVisual.SetActive(currentState != State.Tamed);
        if (tamedVisual != null) tamedVisual.SetActive(currentState == State.Tamed);
        
        // Update layer?
        gameObject.layer = currentState == State.Tamed ? LayerMask.NameToLayer("TamedPlant") : LayerMask.NameToLayer("AntiPlant");
    }
}
