using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform player;
    public float visionRange = 18f;
    [Range(5f, 180f)] public float visionAngle = 80f;
    public LayerMask visionBlockLayers = ~0;
    public float eyeHeight = 1.4f;
    public float playerHeight = 1.2f;
    public float reactionTime = 0.2f;

    [Header("Cone Visual")]
    public MeshRenderer coneRenderer;
    public Color idleColor = new Color(0f, 1f, 1f, 0.2f);
    public Color detectedColor = new Color(1f, 0.2f, 0.2f, 0.35f);

    public bool CanSeePlayer { get; private set; }
    private float seenTimer;

    private void Update()
    {
        bool hasSightNow = EvaluateSight();
        if (hasSightNow) seenTimer += Time.deltaTime; else seenTimer = 0f;
        CanSeePlayer = hasSightNow && seenTimer >= reactionTime;
        UpdateConeVisual();
    }

    private bool EvaluateSight()
    {
        if (player == null) return false;
        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 to = player.position + Vector3.up * playerHeight;
        Vector3 dir = to - from;
        if (dir.magnitude > visionRange) return false;
        if (Vector3.Angle(transform.forward, dir.normalized) > visionAngle * 0.5f) return false;
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, visionRange, visionBlockLayers))
        {
            return hit.collider.GetComponentInParent<PlayerHealth>() != null;
        }
        return false;
    }

    private void UpdateConeVisual()
    {
        if (coneRenderer == null) return;
        Color c = CanSeePlayer ? detectedColor : idleColor;
        coneRenderer.material.color = c;
    }
}
