using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

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

    [Tooltip("How far the nommian will roam")]
    [SerializeField] private float roamRadius;

    [Tooltip("Radius distance to detect the player")]
    [SerializeField] private float detectionRadius;

    [Tooltip("How far the player must be for the nommian to activate")]
    [SerializeField] private float activationRadius = 40f;
    [SerializeField] private float stateTransitionDuration = 1f;

    [Space(10)]

    [Header("Attacking")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackRange = 5f;

    private float activationRadiusSqr;

    private State currentState;

    private Vector3 roamTarget;
    private Transform currentTarget;

    private float detectTimer = 0.2f;

    [HideInInspector] public bool isCaptured = false;
    [HideInInspector] public bool canDamage = false;

    private bool isActive = false;

    private Animator animator;
    private Rigidbody rb;
    private NavMeshAgent agent;

    private float idleState = 0f;
    private float walkState = 1f;
    private float runState = 2f;
    private bool attackTriggered;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        activationRadiusSqr = activationRadius * activationRadius;

        if (!IsServer) return;

        currentState = State.Roaming;
        roamTarget = GetRandomPoint();

        animator.SetFloat("State", idleState);
    }

    void Update()
    {
        if (!IsServer || isCaptured) return;
        
        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0f)
        {
            detectTimer = 0.2f;

            bool shouldBeActive = IsPlayerNearby();

            if (shouldBeActive != isActive)
            {
                ToggleNommian(shouldBeActive);
            }

            if (!isActive) return;

            HandleDetection();
        }

        switch (currentState)
        {
            case State.Roaming:
                StartCoroutine(StateAnimTransition(walkState));
                Roaming();
                break;

            case State.Fleeing:
                StartCoroutine(StateAnimTransition(walkState));
                Fleeing();
                break;

            case State.Chasing:
                StartCoroutine(StateAnimTransition(runState));
                Chasing();
                break;

            case State.Attacking:
                if (!attackTriggered)
                {
                    animator.SetTrigger("Attack");
                    attackTriggered = true;
                }
                break;
        }
    }

    /// Checks if any players are in activation range
    private bool IsPlayerNearby()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;
            
            Vector3 diff = client.PlayerObject.transform.position - transform.position;

            // Using square magnitudes is more performant than Vector3.Distance
            if (diff.sqrMagnitude <= activationRadiusSqr)
                return true;
        }

        return false;
    }

    private void HandleDetection()
    {
        currentTarget = GetClosestPlayer();

        if (currentTarget == null)
        {
            currentState = State.Roaming;
            return;
        }

        if (type == NommianType.Hostile && !attackTriggered)
        {
            if (Vector3.Distance(transform.position, currentTarget.position) > attackRange) currentState = State.Chasing;
            else 
            {
                currentState = State.Attacking;
            }
        }
        else if (type == NommianType.Runner)
        {
            currentState = State.Fleeing;
        }
    }

    private IEnumerator StateAnimTransition(float newState)
    {
        float elapsed = 0f;
        float value = animator.GetFloat("State");

        // Smoothly transitions between animator states
        while (elapsed <= stateTransitionDuration)
        {
            elapsed += Time.deltaTime;
            value = Mathf.Lerp(value, newState, elapsed / stateTransitionDuration);
            animator.SetFloat("State", value);
            yield return null;
        }
        animator.SetFloat("State", newState);
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

    // Chases the player
    private void Chasing()
    {
        if (currentTarget == null) return;
        agent.speed = speed * speedMultiplier;

        if (NavMesh.SamplePosition(currentTarget.position, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void AllowAttack()
    {
        canDamage = true;
    }

    public void DisableAttack()
    {
        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        currentState = State.Fleeing;
        canDamage = false;
        yield return new WaitForSeconds(attackCooldown);
        attackTriggered = false;
    }

    private Transform GetClosestPlayer()
    {
        float minDist = detectionRadius;
        Transform closest = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

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

    /// Toggles the nommian on/off
    public void ToggleNommian(bool active)
    {
        agent.isStopped = !active;
        animator.enabled = active;
        rb.isKinematic = !active;
        isActive = active;
    }

    // Damage shouldn't be public as many other scripts use damage as a var
    public float GetNommianDamage()
    {
        return damage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, roamRadius);
    }
}
