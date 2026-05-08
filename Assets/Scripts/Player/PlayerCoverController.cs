using UnityEngine;

/// <summary>
/// Simple cover controller for the prototype.
/// 
/// Press E to enter/exit cover.
/// Hold RMB while in cover to peek and allow shooting.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerCoverController : MonoBehaviour
{
    [Header("Cover Search")]
    public float coverSearchRadius = 3f;
    public LayerMask coverLayerMask = ~0;

    [Header("Cover Positioning")]
    [Tooltip("How far outside the cover face the player should snap.")]
    public float faceOffset = 0.65f;

    [Header("Player Scale")]
    public float hiddenScaleY = 0.55f;

    [Header("Camera")]
    public Transform playerCameraTransform;
    public float hiddenCameraYOffset = -0.45f;

    [Header("Movement")]
    [Tooltip("When in cover and not peeking, player movement speed is multiplied by this value.")]
    [Range(0f, 1f)]
    public float hiddenMovementMultiplier = 0f;

    public bool IsInCover { get; private set; }
    public bool IsPeekingFromCover { get; private set; }

    public float TimeInCoverSeconds { get; private set; }
    public float TimePeekingSeconds { get; private set; }

    private Transform activeCover;
    private Vector3 activeCoverSnapPosition;

    private Vector3 originalScale;
    private Vector3 originalCameraLocalPosition;

    private PlayerController playerController;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (playerCameraTransform != null)
        {
            originalCameraLocalPosition = playerCameraTransform.localPosition;
        }

        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsInCover) ExitCover();
            else TryEnterCover();
        }

        if (IsInCover)
        {
            UpdatePeekState();
            KeepSnappedToCover();
            UpdateCoverTimers();
        }
    }

    public float GetMovementMultiplier()
    {
        if (!IsInCover) return 1f;
        return IsPeekingFromCover ? 1f : hiddenMovementMultiplier;
    }

    private void TryEnterCover()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, coverSearchRadius, coverLayerMask);

        Collider nearestCoverCollider = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < nearby.Length; i++)
        {
            if (!nearby[i].CompareTag("Cover")) continue;

            float distance = Vector3.Distance(transform.position, nearby[i].bounds.ClosestPoint(transform.position));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCoverCollider = nearby[i];
            }
        }

        if (nearestCoverCollider == null) return;

        activeCover = nearestCoverCollider.transform;
        activeCoverSnapPosition = CalculateSnapPosition(nearestCoverCollider);

        IsInCover = true;
        IsPeekingFromCover = false;

        ApplyHiddenCoverPose();
        KeepSnappedToCover();
    }

    private Vector3 CalculateSnapPosition(Collider coverCollider)
    {
        Bounds bounds = coverCollider.bounds;
        Vector3 playerPosition = transform.position;

        float distanceToLeft = Mathf.Abs(playerPosition.x - bounds.min.x);
        float distanceToRight = Mathf.Abs(playerPosition.x - bounds.max.x);
        float distanceToBack = Mathf.Abs(playerPosition.z - bounds.min.z);
        float distanceToFront = Mathf.Abs(playerPosition.z - bounds.max.z);

        float closest = Mathf.Min(distanceToLeft, distanceToRight, distanceToBack, distanceToFront);

        Vector3 snap = playerPosition;

        if (closest == distanceToLeft) snap = new Vector3(bounds.min.x - faceOffset, playerPosition.y, Mathf.Clamp(playerPosition.z, bounds.min.z, bounds.max.z));
        else if (closest == distanceToRight) snap = new Vector3(bounds.max.x + faceOffset, playerPosition.y, Mathf.Clamp(playerPosition.z, bounds.min.z, bounds.max.z));
        else if (closest == distanceToBack) snap = new Vector3(Mathf.Clamp(playerPosition.x, bounds.min.x, bounds.max.x), playerPosition.y, bounds.min.z - faceOffset);
        else snap = new Vector3(Mathf.Clamp(playerPosition.x, bounds.min.x, bounds.max.x), playerPosition.y, bounds.max.z + faceOffset);

        return snap;
    }

    private void UpdatePeekState()
    {
        bool peekInput = Input.GetMouseButton(1);

        if (peekInput && !IsPeekingFromCover)
        {
            IsPeekingFromCover = true;
            ApplyPeekingPose();
        }
        else if (!peekInput && IsPeekingFromCover)
        {
            IsPeekingFromCover = false;
            ApplyHiddenCoverPose();
        }
    }

    private void KeepSnappedToCover()
    {
        transform.position = new Vector3(activeCoverSnapPosition.x, transform.position.y, activeCoverSnapPosition.z);
    }

    private void ApplyHiddenCoverPose()
    {
        Vector3 hiddenScale = originalScale;
        hiddenScale.y = originalScale.y * hiddenScaleY;
        transform.localScale = hiddenScale;

        if (playerCameraTransform != null)
        {
            playerCameraTransform.localPosition = originalCameraLocalPosition + new Vector3(0f, hiddenCameraYOffset, 0f);
        }
    }

    private void ApplyPeekingPose()
    {
        transform.localScale = originalScale;

        if (playerCameraTransform != null)
        {
            playerCameraTransform.localPosition = originalCameraLocalPosition;
        }
    }

    private void ExitCover()
    {
        IsInCover = false;
        IsPeekingFromCover = false;
        activeCover = null;

        transform.localScale = originalScale;

        if (playerCameraTransform != null)
        {
            playerCameraTransform.localPosition = originalCameraLocalPosition;
        }
    }

    private void UpdateCoverTimers()
    {
        TimeInCoverSeconds += Time.deltaTime;

        if (IsPeekingFromCover)
        {
            TimePeekingSeconds += Time.deltaTime;
        }
    }
}
