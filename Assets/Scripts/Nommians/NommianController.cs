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
    [SerializeField] private float roamRadius;

    [Tooltip("Radius distance to detect the player")]
    [SerializeField] private float detectionRadius;

    private NavMeshAgent agent;
    private State currentState;
    private Vector3 target;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer) return;

        currentState = State.Roaming;
        target = PickNewRoamPoint();
    }

    void Update()
    {
        if (!IsServer) return;

        switch (currentState)
        {
            case State.Roaming:
                target = PickNewRoamPoint();
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

        if (Vector3.Distance(transform.position, target) < 1f)
        {
            Debug.Log("Reached target");
            target = PickNewRoamPoint();
        }
        
        agent.SetDestination(target);
    }

    // Randomly moves around
    private void Fleeing()
    {
        agent.speed = speed;
    }

    // Randomly moves around
    private void Chasing()
    {
        agent.speed = speed;
    }

    // Randomly moves around
    private void Attacking()
    {
        agent.speed = speed;
    }

    Vector3 PickNewRoamPoint()
    {
        Vector2 random = Random.insideUnitCircle * roamRadius;

        Vector3 point = new Vector3(
            transform.position.x + random.x,
            transform.position.y,
            transform.position.z + random.y
        );

        // Checks that the chosen position is valid
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    void OnDrawGizmosSelected()
    {
        DrawWireSphere()
    }
}
