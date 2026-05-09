using UnityEngine;

/// <summary>
/// Very simple line-of-sight check used by enemies.
/// It only reports whether the player is currently visible.
/// </summary>
public class EnemyLineOfSight : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Vision")]
    public float visionRange = 20f;
    [Range(1f, 360f)]
    public float visionAngle = 90f;
    public float eyeHeight = 1.3f;

    [Tooltip("Layers that can block visibility (for example: walls and cover).")]
    public LayerMask obstacleLayers;

    public bool CanSeePlayer { get; private set; }

    private void Update()
    {
        CanSeePlayer = EvaluateLineOfSight();
    }

    public void Setup(Transform playerTransform)
    {
        player = playerTransform;
    }

    private bool EvaluateLineOfSight()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 playerAimPoint = player.position + Vector3.up * eyeHeight;

        Vector3 toPlayer = playerAimPoint - eyePosition;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > visionRange)
        {
            return false;
        }

        Vector3 directionToPlayer = toPlayer.normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > visionAngle * 0.5f)
        {
            return false;
        }

        bool blocked = Physics.Raycast(
            eyePosition,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;
    }

    private void OnDrawGizmos()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Color visionColor = CanSeePlayer ? Color.red : Color.green;

        Gizmos.color = visionColor;

        Vector3 leftBoundary = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary * visionRange);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary * visionRange);

        const int segments = 16;
        Vector3 previousPoint = eyePosition + leftBoundary * visionRange;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = Mathf.Lerp(-visionAngle * 0.5f, visionAngle * 0.5f, t);
            Vector3 direction = Quaternion.Euler(0f, currentAngle, 0f) * transform.forward;
            Vector3 point = eyePosition + direction * visionRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        if (player != null)
        {
            Gizmos.DrawLine(eyePosition, player.position + Vector3.up * eyeHeight);
        }
    }
}
