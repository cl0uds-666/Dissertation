using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyMovementMode { None, PatrolSide, ChasePlayer, RushPlayer, MoveToCover, SuppressAndMove, BlockPath }
    public Transform player;
    public EnemyMovementMode movementMode;
    public float patrolSpeed = 2.2f;
    public float chaseSpeed = 3.4f;
    public float rushSpeed = 5f;
    public float contactDamage = 10f;
    public float damageCooldown = 1f;
    public float waypointTolerance = 1f;
    public Vector3 blockPoint;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private EnemyVision vision;
    private Vector3 lastKnownPlayerPosition;
    private float nextDamageTime;
    private int patrolIndex;
    private Vector3[] patrolPoints;

    private void Awake(){ rb=GetComponent<Rigidbody>(); agent=GetComponent<NavMeshAgent>(); vision=GetComponent<EnemyVision>(); rb.isKinematic=true; rb.useGravity=false; rb.constraints=RigidbodyConstraints.FreezeRotation; }

    private void Update()
    {
        if (player != null) lastKnownPlayerPosition = player.position;
        switch (movementMode)
        {
            case EnemyMovementMode.None: agent.isStopped = true; break;
            case EnemyMovementMode.PatrolSide: Patrol(); break;
            case EnemyMovementMode.ChasePlayer: Chase(chaseSpeed); break;
            case EnemyMovementMode.RushPlayer: Chase(rushSpeed); break;
            case EnemyMovementMode.MoveToCover: MoveToPoint(blockPoint, patrolSpeed); break;
            case EnemyMovementMode.SuppressAndMove: Patrol(); break;
            case EnemyMovementMode.BlockPath: MoveToPoint(blockPoint, chaseSpeed); break;
        }
    }

    private void Patrol(){ if (patrolPoints == null || patrolPoints.Length == 0){ agent.isStopped=true; return;} agent.speed=patrolSpeed; agent.isStopped=false; if(!agent.hasPath) agent.SetDestination(patrolPoints[patrolIndex]); if(!agent.pathPending && agent.remainingDistance<=waypointTolerance){patrolIndex=(patrolIndex+1)%patrolPoints.Length; agent.SetDestination(patrolPoints[patrolIndex]);}}
    private void Chase(float speed){ if(player==null){agent.isStopped=true; return;} bool canSee = vision == null || vision.CanSeePlayer; agent.speed = speed; agent.isStopped=false; agent.SetDestination(canSee ? player.position : lastKnownPlayerPosition); }
    private void MoveToPoint(Vector3 p,float speed){agent.speed=speed; agent.isStopped=false; agent.SetDestination(p);}    

    public void Setup(Transform playerTransform, EnemyMovementMode mode, float patrol, float chase, float rush, Vector3[] patrolRoute, Vector3 targetPoint)
    { player=playerTransform; movementMode=mode; patrolSpeed=patrol; chaseSpeed=chase; rushSpeed=rush; patrolPoints=patrolRoute; blockPoint=targetPoint; if (vision!=null) vision.player = playerTransform; }

    private void OnCollisionStay(Collision collision)
    {
        if (movementMode == EnemyMovementMode.None || Time.time < nextDamageTime) return;
        PlayerHealth ph = collision.collider.GetComponent<PlayerHealth>(); if (ph == null) return; ph.TakeDamage(contactDamage); nextDamageTime = Time.time + damageCooldown;
    }
}
