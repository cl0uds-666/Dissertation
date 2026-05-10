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

    // Optional line-of-sight component for simple visibility-driven behavior.
    private EnemyLineOfSight enemyLineOfSight;

    // Stores where the player was when they were last visible.
    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPlayerPosition;

    // Tracks visibility changes so we only do "go to last known position" once.
    private bool wasSeeingPlayerLastFrame;
    private bool isSearchingLastKnownPosition;

    // Simple fallback patrol anchor when no patrol points are configured.
    private Vector3 defaultPatrolAnchor;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.stoppingDistance = stopDistance;
        defaultPatrolAnchor = transform.position;

        enemyLineOfSight = GetComponent<EnemyLineOfSight>();

        // Helpful auto-wiring: if LOS exists but has no player, use our player reference.
        if (enemyLineOfSight != null && enemyLineOfSight.player == null && player != null)
        {
            enemyLineOfSight.Setup(player);
        }
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

                // Only look at player when detected if LOS is available.
                if (enemyLineOfSight == null || enemyLineOfSight.player == null || enemyLineOfSight.CanSeePlayer)
                {
                    FacePlayer();
                }
                break;

            case MovementMode.Patrol:
                UpdatePatrolOrLookMovement();
                break;

            case MovementMode.ChasePlayer:
                UpdateChaseMovement();
                break;
        }
    }


    private void UpdatePatrolOrLookMovement()
    {
        // If LOS exists and the player is visible, stop and look so shooters can fire.
        if (enemyLineOfSight != null && enemyLineOfSight.player != null && enemyLineOfSight.CanSeePlayer)
        {
            navMeshAgent.isStopped = true;
            FacePlayer();
            return;
        }

        UpdatePatrolMovement();
    }

    private void UpdateChaseMovement()
    {
        // Fallback: if no line-of-sight component exists, keep old chase behavior.
        if (enemyLineOfSight == null)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.SetDestination(player.position);
            return;
        }

        // If LOS has no player assigned, fail safe to old chase behavior.
        if (enemyLineOfSight.player == null)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.SetDestination(player.position);
            return;
        }

        bool canSeePlayer = enemyLineOfSight.CanSeePlayer;

        // When the player is visible, keep updating last known position and chase.
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            hasLastKnownPlayerPosition = true;
            wasSeeingPlayerLastFrame = true;
            isSearchingLastKnownPosition = false;

            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.SetDestination(player.position);
            return;
        }

        // If visibility just changed to false, move once to last known position.
        if (wasSeeingPlayerLastFrame && hasLastKnownPlayerPosition)
        {
            isSearchingLastKnownPosition = true;
            wasSeeingPlayerLastFrame = false;

            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = 0f;
            navMeshAgent.SetDestination(lastKnownPlayerPosition);
            return;
        }

        // While searching, continue until that point is reached.
        if (isSearchingLastKnownPosition)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = 0f;

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= patrolPointReachedDistance)
            {
                isSearchingLastKnownPosition = false;
            }
            else
            {
                return;
            }
        }

        // If player is not visible and search is done, patrol.
        UpdatePatrolMovement();
    }

    private void UpdatePatrolMovement()
    {
        if (patrolPoints.Count == 0)
        {
            // No patrol route assigned: move back to spawn anchor as a simple patrol fallback.
            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = 0f;
            navMeshAgent.SetDestination(defaultPatrolAnchor);

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= patrolPointReachedDistance)
            {
                navMeshAgent.isStopped = true;
            }

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

        if (enemyLineOfSight != null && enemyLineOfSight.player == null && player != null)
        {
            enemyLineOfSight.Setup(player);
        }

        patrolPoints.Clear();

        if (newPatrolPoints != null)
        {
            patrolPoints.AddRange(newPatrolPoints);
        }

        currentPatrolIndex = 0;

        defaultPatrolAnchor = transform.position;

        hasLastKnownPlayerPosition = false;
        wasSeeingPlayerLastFrame = false;
        isSearchingLastKnownPosition = false;

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
