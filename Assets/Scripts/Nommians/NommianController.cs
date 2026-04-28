using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

public class NommianController : NetworkBehaviour
{
    private enum State
    {
        Roaming,
        Chasing,
        Fleeing,
        Attacking
    }

    public enum NommianType
    {
        Hostile,
        Runner
    }

    [Header("Basic Info")]
    [SerializeField] private NommianType type;
    [SerializeField] private float speed;
    [SerializeField] private float speedMultiplier;

    [Tooltip("Radius distance to detect the player")]
    [SerializeField] private float detectionRadius;

    private NavMeshAgent agent;
    private State currentState;
    private Transform target;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer) return;

        currentState = State.Roaming;
    }

    void Update()
    {
        if (!IsServer) return;

        switch (currentState)
        {
            case State.Roaming:
                Roaming();
                break;
            case State.Fleeing:
                Fleeing();
                break;
            case State.Chasing:
                Chasing();
                break;
            case State.Attacking:
                Attacking();
                break;
        }
    }

    private void HandleDetection()
    {

    }

    // Randomly moves around
    private void Roaming()
    {
        agent.speed = speed;
    }
}
