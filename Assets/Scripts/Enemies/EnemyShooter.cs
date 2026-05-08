using UnityEngine;

/// <summary>
/// Handles enemy ranged attacks.
/// 
/// The enemy fires a SphereCast toward the player. Accuracy is controlled
/// using random spread: lower spread means more accurate enemies.
/// 
/// Cover can block enemy shots because the SphereCast hits the first collider
/// between the enemy and the player.
/// </summary>
public class EnemyShooter : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Shooting")]
    public bool canShoot = true;

    [Tooltip("How much damage each successful shot deals.")]
    public float damage = 10f;

    [Tooltip("Seconds between enemy shots.")]
    public float fireCooldown = 1.5f;

    [Tooltip("Maximum shooting distance.")]
    public float shotRange = 35f;

    [Tooltip("Thickness of the enemy shot.")]
    public float shotRadius = 0.15f;

    [Tooltip("Random shot spread. Higher = less accurate.")]
    public float shotSpread = 0.2f;

    [Header("Line of Sight")]
    [Tooltip("Height offset for enemy shooting origin.")]
    public float enemyEyeHeight = 1.3f;

    [Tooltip("Height offset for aiming at the player.")]
    public float playerAimHeight = 1.2f;

    [Header("Cover Interaction")]
    [Range(0f, 1f)]
    public float peekDamageChance = 0.5f;

    [Header("Debug")]
    public bool drawDebugRay = true;

    private float nextFireTime;

    public void Setup(
        Transform playerTransform,
        float newDamage,
        float newFireCooldown,
        float newShotRange,
        float newShotRadius,
        float newShotSpread,
        bool shootingEnabled,
        float newPeekDamageChance
    )
    {
        player = playerTransform;

        damage = newDamage;
        fireCooldown = newFireCooldown;
        shotRange = newShotRange;
        shotRadius = newShotRadius;
        shotSpread = newShotSpread;

        canShoot = shootingEnabled;
        peekDamageChance = Mathf.Clamp01(newPeekDamageChance);
    }

    private void Update()
    {
        if (!canShoot)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        TryShootPlayer();

        nextFireTime = Time.time + fireCooldown;
    }

    private void TryShootPlayer()
    {
        Vector3 shotOrigin = transform.position + Vector3.up * enemyEyeHeight;

        Vector3 targetPosition = player.position + Vector3.up * playerAimHeight;

        Vector3 directionToPlayer = targetPosition - shotOrigin;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > shotRange)
        {
            return;
        }

        Vector3 shotDirection = directionToPlayer.normalized;

        shotDirection = ApplySpread(shotDirection);

        if (drawDebugRay)
        {
            Debug.DrawRay(shotOrigin, shotDirection * shotRange, Color.yellow, 0.2f);
        }

        RaycastHit hit;

        bool hasHit = Physics.SphereCast(
            shotOrigin,
            shotRadius,
            shotDirection,
            out hit,
            shotRange
        );

        if (!hasHit)
        {
            return;
        }

        PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            return;
        }

        PlayerCoverController coverController = playerHealth.GetComponent<PlayerCoverController>();

        if (coverController == null || !coverController.IsInCover)
        {
            playerHealth.TakeDamage(damage);
            return;
        }

        if (!coverController.IsPeekingFromCover)
        {
            return;
        }

        if (Random.value <= peekDamageChance)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    private Vector3 ApplySpread(Vector3 originalDirection)
    {
        Vector3 randomSpread = new Vector3(
            Random.Range(-shotSpread, shotSpread),
            Random.Range(-shotSpread, shotSpread),
            Random.Range(-shotSpread, shotSpread)
        );

        Vector3 finalDirection = originalDirection + randomSpread;

        return finalDirection.normalized;
    }
}