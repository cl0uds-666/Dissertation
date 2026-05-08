using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerCoverController : MonoBehaviour
{
    public enum CoverPeekMode { None, Hidden, PeekOver, PeekSide }
    public enum CoverHeightType { None, Low, Tall }

    [Header("Cover Search")]
    public float coverSearchRadius = 3f;
    public LayerMask coverLayerMask = ~0;

    [Header("Cover Positioning")]
    public float faceOffset = 0.65f;

    [Header("Height Aware Peek")]
    public float lowCoverMaxHeight = 1.35f;
    public float peekOverCameraHeight = 0f;
    public float hiddenCameraHeight = -0.45f;
    public float sidePeekOffset = 0.65f;
    public float overPeekHeightOffset = 0f;

    [Header("Player Scale")]
    public float hiddenScaleY = 0.55f;

    [Header("Camera")]
    public Transform playerCameraTransform;

    [Header("Movement")]
    [Range(0f, 1f)] public float hiddenMovementMultiplier = 0f;
    public float coverSlideSpeed = 4f;

    [Header("Debug")]
    public bool logCoverDebug = true;

    public bool IsInCover { get; private set; }
    public bool IsPeekingFromCover { get; private set; }
    public CoverPeekMode CurrentPeekMode { get; private set; } = CoverPeekMode.None;
    public CoverHeightType CurrentCoverType { get; private set; } = CoverHeightType.None;
    public float TimeInCoverSeconds { get; private set; }
    public float TimePeekingSeconds { get; private set; }

    private Collider activeCoverCollider;
    private Vector3 activeCoverSnapPosition;
    private Vector3 coverFaceNormal;
    private Vector3 coverSlideDirection;

    private Vector3 originalScale;
    private Vector3 originalCameraLocalPosition;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (playerCameraTransform != null) originalCameraLocalPosition = playerCameraTransform.localPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsInCover) ExitCover(); else TryEnterCover();
        }

        if (!IsInCover) return;
        if (activeCoverCollider == null) { ExitCover(); return; }

        UpdateCoverSlide();
        UpdatePeekState();
        KeepSnappedToCover();
        UpdateCoverTimers();
    }

    public float GetMovementMultiplier() => !IsInCover ? 1f : (IsPeekingFromCover ? 1f : hiddenMovementMultiplier);

    private void TryEnterCover()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, coverSearchRadius, coverLayerMask);
        Collider nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < nearby.Length; i++)
        {
            if (!nearby[i].CompareTag("Cover")) continue;
            float d = Vector3.Distance(transform.position, nearby[i].bounds.ClosestPoint(transform.position));
            if (d < nearestDistance) { nearestDistance = d; nearest = nearby[i]; }
        }
        if (nearest == null) return;

        activeCoverCollider = nearest;
        activeCoverSnapPosition = CalculateSnapPosition(nearest);
        CurrentCoverType = nearest.bounds.size.y <= lowCoverMaxHeight ? CoverHeightType.Low : CoverHeightType.Tall;

        IsInCover = true;
        IsPeekingFromCover = false;
        CurrentPeekMode = CoverPeekMode.Hidden;
        ApplyHiddenCoverPose();
    }

    private Vector3 CalculateSnapPosition(Collider coverCollider)
    {
        Bounds b = coverCollider.bounds;
        Vector3 p = transform.position;
        float left = Mathf.Abs(p.x - b.min.x), right = Mathf.Abs(p.x - b.max.x), back = Mathf.Abs(p.z - b.min.z), front = Mathf.Abs(p.z - b.max.z);
        float closest = Mathf.Min(left, right, back, front);
        if (closest == left) { coverFaceNormal = Vector3.left; coverSlideDirection = Vector3.forward; return new Vector3(b.min.x - faceOffset, p.y, Mathf.Clamp(p.z, b.min.z, b.max.z)); }
        if (closest == right) { coverFaceNormal = Vector3.right; coverSlideDirection = Vector3.forward; return new Vector3(b.max.x + faceOffset, p.y, Mathf.Clamp(p.z, b.min.z, b.max.z)); }
        if (closest == back) { coverFaceNormal = Vector3.back; coverSlideDirection = Vector3.right; return new Vector3(Mathf.Clamp(p.x, b.min.x, b.max.x), p.y, b.min.z - faceOffset); }
        coverFaceNormal = Vector3.forward; coverSlideDirection = Vector3.right; return new Vector3(Mathf.Clamp(p.x, b.min.x, b.max.x), p.y, b.max.z + faceOffset);
    }

    private void UpdateCoverSlide()
    {
        float input = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(input) < 0.01f) return;
        activeCoverSnapPosition += coverSlideDirection * input * coverSlideSpeed * Time.deltaTime;

        Bounds b = activeCoverCollider.bounds;
        if (Mathf.Abs(coverFaceNormal.x) > 0.5f) activeCoverSnapPosition.z = Mathf.Clamp(activeCoverSnapPosition.z, b.min.z, b.max.z);
        if (Mathf.Abs(coverFaceNormal.z) > 0.5f) activeCoverSnapPosition.x = Mathf.Clamp(activeCoverSnapPosition.x, b.min.x, b.max.x);
    }

    private void UpdatePeekState()
    {
        bool peek = Input.GetMouseButton(1);
        IsPeekingFromCover = peek;
        if (!peek) { CurrentPeekMode = CoverPeekMode.Hidden; ApplyHiddenCoverPose(); return; }

        if (CurrentCoverType == CoverHeightType.Low)
        {
            CurrentPeekMode = CoverPeekMode.PeekOver;
            ApplyPeekOverPose();
        }
        else
        {
            CurrentPeekMode = CoverPeekMode.PeekSide;
            ApplyPeekSidePose();
        }
    }

    private void KeepSnappedToCover() => transform.position = new Vector3(activeCoverSnapPosition.x, transform.position.y, activeCoverSnapPosition.z);

    private void ApplyHiddenCoverPose()
    {
        Vector3 s = originalScale; s.y = originalScale.y * hiddenScaleY; transform.localScale = s;
        if (playerCameraTransform != null) playerCameraTransform.localPosition = originalCameraLocalPosition + new Vector3(0f, hiddenCameraHeight, 0f);
    }

    private void ApplyPeekOverPose()
    {
        transform.localScale = originalScale;
        if (playerCameraTransform != null) playerCameraTransform.localPosition = originalCameraLocalPosition + new Vector3(0f, peekOverCameraHeight + overPeekHeightOffset, 0f);
    }

    private void ApplyPeekSidePose()
    {
        transform.localScale = originalScale;
        if (playerCameraTransform == null) return;

        float sideSign = GetNearestSideSign();
        Vector3 sideDir = Vector3.Cross(Vector3.up, coverFaceNormal).normalized;
        playerCameraTransform.localPosition = originalCameraLocalPosition + new Vector3(0f, peekOverCameraHeight, 0f) + sideDir * (sidePeekOffset * sideSign);
    }

    private float GetNearestSideSign()
    {
        Bounds b = activeCoverCollider.bounds;
        if (Mathf.Abs(coverFaceNormal.x) > 0.5f)
        {
            float leftDist = Mathf.Abs(activeCoverSnapPosition.z - b.min.z);
            float rightDist = Mathf.Abs(activeCoverSnapPosition.z - b.max.z);
            return leftDist <= rightDist ? -1f : 1f;
        }

        float minDist = Mathf.Abs(activeCoverSnapPosition.x - b.min.x);
        float maxDist = Mathf.Abs(activeCoverSnapPosition.x - b.max.x);
        return minDist <= maxDist ? -1f : 1f;
    }

    private void ExitCover()
    {
        IsInCover = false; IsPeekingFromCover = false;
        CurrentPeekMode = CoverPeekMode.None; CurrentCoverType = CoverHeightType.None;
        activeCoverCollider = null;
        transform.localScale = originalScale;
        if (playerCameraTransform != null) playerCameraTransform.localPosition = originalCameraLocalPosition;
    }

    private void UpdateCoverTimers()
    {
        TimeInCoverSeconds += Time.deltaTime;
        if (IsPeekingFromCover) TimePeekingSeconds += Time.deltaTime;
    }
}
