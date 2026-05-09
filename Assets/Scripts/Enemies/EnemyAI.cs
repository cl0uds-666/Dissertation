using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Basic enemy movement for the dissertation prototype.
///
/// Movement uses NavMeshAgent so enemies can path around generated cover.
/// Shooting is still handled by EnemyShooter.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum MovementMode
    {
        Stationary,
        Patrol,
        ChasePlayer
    }

    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public MovementMode movementMode = MovementMode.Stationary;
    public float moveSpeed = 3f;

    [Tooltip("Enemy stops moving when this close to the player.")]
    public float stopDistance = 1.5f;

    [Tooltip("How close the enemy must get before moving to the next patrol point.")]
    public float patrolPointReachedDistance = 0.8f;

    [Header("Damage")]
    public float contactDamage = 10f;
    public float damageCooldown = 1f;

    private NavMeshAgent navMeshAgent;
    private readonly List<Vector3> patrolPoints = new List<Vector3>();
    private int currentPatrolIndex;
    private float nextDamageTime;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.stoppingDistance = stopDistance;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        switch (movementMode)
        {
            case MovementMode.Stationary:
                navMeshAgent.isStopped = true;
                FacePlayer();
                break;

            case MovementMode.Patrol:
                UpdatePatrolMovement();
                break;

            case MovementMode.ChasePlayer:
                UpdateChaseMovement();
                break;
        }
    }

    private void UpdateChaseMovement()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.stoppingDistance = stopDistance;
        navMeshAgent.SetDestination(player.position);
    }

    private void UpdatePatrolMovement()
    {
        if (patrolPoints.Count == 0)
        {
            navMeshAgent.isStopped = true;
            FacePlayer();
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.stoppingDistance = 0f;

        if (!navMeshAgent.hasPath)
        {
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex]);
            return;
        }

        if (navMeshAgent.remainingDistance <= patrolPointReachedDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex]);
        }
    }

    private void FacePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(directionToPlayer.normalized);
    }

    public void Setup(Transform playerTransform, float speed, MovementMode mode, List<Vector3> newPatrolPoints = null)
    {
        player = playerTransform;
        moveSpeed = speed;
        movementMode = mode;

        navMeshAgent.speed = moveSpeed;
        navMeshAgent.stoppingDistance = stopDistance;

        patrolPoints.Clear();

        if (newPatrolPoints != null)
        {
            patrolPoints.AddRange(newPatrolPoints);
        }

        currentPatrolIndex = 0;

        if (movementMode == MovementMode.Patrol && patrolPoints.Count > 0)
        {
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex]);
        }
        else
        {
            navMeshAgent.ResetPath();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (movementMode == MovementMode.Stationary)
        {
            return;
        }

        if (Time.time < nextDamageTime)
        {
            return;
        }

        PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(contactDamage);

        nextDamageTime = Time.time + damageCooldown;
    }
}
