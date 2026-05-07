using UnityEngine;

/// <summary>
/// Basic enemy movement for the dissertation prototype.
/// 
/// Movement can be disabled, allowing the same enemy prefab to be used
/// as either a moving chaser or a static shooter.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public bool canMove = true;

    public float moveSpeed = 3f;

    [Tooltip("Enemy stops moving when this close to the player.")]
    public float stopDistance = 1.5f;

    [Header("Damage")]
    public float contactDamage = 10f;
    public float damageCooldown = 1f;

    private Rigidbody rb;

    private float nextDamageTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Stops the capsule from tipping over.
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            return;
        }

        FacePlayer();

        if (canMove)
        {
            MoveTowardsPlayer();
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

    private void MoveTowardsPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= stopDistance)
        {
            return;
        }

        Vector3 moveDirection = directionToPlayer.normalized;

        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPosition);
    }

    public void Setup(Transform playerTransform, float speed, bool movementEnabled)
    {
        player = playerTransform;
        moveSpeed = speed;
        canMove = movementEnabled;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!canMove)
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