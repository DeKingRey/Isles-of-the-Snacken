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

    private Vector3 roamTarget;
    private Transform currentTarget;

    private float detectTimer = 0.2f;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer) return;

        currentState = State.Roaming;
        roamTarget = PickNewRoamPoint();
    }

    void Update()
    {
        if (!IsServer) return;

        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0f)
        {
            detectTimer = 0.2f;
            HandleDetection();
        }

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
        currentTarget = GetClosestPlayer();

        if (currentTarget == null)
        {
            currentState = State.Roaming;
            return;
        }

        if (type == NommianType.Hostile)
        {
            currentState = State.Chasing;
        }
        else if (type == NommianType.Runner)
        {
            currentState = State.Fleeing;
        }
    }

    // Randomly moves around
    private void Roaming()
    {
        agent.speed = speed;

        if (Vector3.Distance(transform.position, roamTarget) < 1f)
        {
            roamTarget = PickNewRoamPoint();
        }
        
        agent.SetDestination(roamTarget);
    }

    // Randomly moves around
    private void Fleeing()
    {
        Vector3 dir = (transform.position - currentTarget.position).normalized;
        Vector3 fleePoint = transform.position + dir * roamRadius;

        agent.speed = speed * speedMultiplier;
        agent.SetDestination(fleePoint);
    }

    // Randomly moves around
    private void Chasing()
    {
        agent.speed = speed * speedMultiplier;
    }

    // Randomly moves around
    private void Attacking()
    {
        agent.speed = speed;
    }

    private Transform GetClosestPlayer()
    {
        float minDist = detectionRadius;
        Transform closest = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            float dist = Vector3.Distance(transform.position, client.PlayerObject.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = client.PlayerObject.transform;
            }
        }

        return closest;
    }

    private Vector3 PickNewRoamPoint()
    {
        Vector2 random = Random.insideUnitCircle * roamRadius;

        Vector3 point = new Vector3(
            transform.position.x + random.x,
            transform.position.y,
            transform.position.z + random.y
        );

        // Checks that the chosen position is valid
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            if (IsReachable(hit.position))
                return hit.position;
        }
        return transform.position;
    }

    private bool IsReachable(Vector3 target)
    {
        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(target, path))
            return false;
        
        return path.status == NavMeshPathStatus.PathComplete;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, roamRadius);
    }
}
