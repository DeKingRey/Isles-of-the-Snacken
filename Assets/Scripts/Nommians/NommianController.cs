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
        roamTarget = GetRandomPoint();
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
            roamTarget = GetRandomPoint();
        }
        
        agent.SetDestination(roamTarget);
    }

    // Runs away from the player
    private void Fleeing()
    {
        if (currentTarget == null) return;
        agent.speed = speed * speedMultiplier;

        Vector3 bestPoint = transform.position;
        float bestDistance = float.MinValue;

        Vector3 awayDir = (transform.position - currentTarget.position).normalized;

        // Gets the furthest away point in a variety of directions
        for (int i =  0; i < 8; i++)
        {
            // Slightly random direction
            Vector3 dir = Quaternion.Euler(0, Random.Range(-60f, 60f), 0) * awayDir;

            Vector3 candidate = transform.position + dir * roamRadius;

            if (!IsValidPosition(candidate))
                continue;

            NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, NavMesh.AllAreas);
            
            float distance = Vector3.Distance(hit.position, currentTarget.position);

            // Finds the furthest distance
            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestPoint = hit.position;
            }
        }

        agent.SetDestination(bestPoint);
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

    private Vector3 GetRandomPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 random = Random.insideUnitCircle * roamRadius;

            Vector3 candidate = new Vector3(
                transform.position.x + random.x,
                transform.position.y,
                transform.position.z + random.y
            );

            if (IsValidPosition(candidate))
            {
                NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, NavMesh.AllAreas);
                return hit.position;
            }
        }
        return transform.position;
    }

    private bool IsValidPosition(Vector3 point)
    {
        // Checks that point is valid position on navmesh
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            return false;
        
        NavMeshPath path = new NavMeshPath();
        
        // Ensures that path can be reached
        if (!agent.CalculatePath(hit.position, path))
            return false;
        
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;
        
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, roamRadius);
    }
}
