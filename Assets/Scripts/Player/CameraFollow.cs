using UnityEngine;

/// <summary>
/// Third-person camera rig for the dissertation prototype.
/// 
/// Mouse movement rotates the camera around the player:
/// - Mouse X controls horizontal rotation.
/// - Mouse Y controls vertical pitch, clamped so the camera cannot flip.
/// 
/// The CameraRig follows the player position, while the Main Camera sits as
/// a child object behind and above the player.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player object the camera should follow.")]
    public Transform target;

    [Header("Mouse Look")]
    [Tooltip("Horizontal mouse sensitivity.")]
    public float mouseSensitivityX = 120f;

    [Tooltip("Vertical mouse sensitivity.")]
    public float mouseSensitivityY = 80f;

    [Header("Pitch Clamp")]
    [Tooltip("Lowest vertical camera angle.")]
    public float minPitch = -10f;

    [Tooltip("Highest vertical camera angle.")]
    public float maxPitch = 60f;

    [Header("Follow Settings")]
    [Tooltip("If true, camera rig smoothly follows the player.")]
    public bool useSmoothing = true;

    [Tooltip("Higher values make the camera catch up faster.")]
    public float followSmoothness = 15f;

    private float yaw;
    private float pitch = 35f;

    private void Start()
    {
        // Lock the cursor so mouse movement controls the camera properly.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start facing the same direction as the rig already faces.
        yaw = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleMouseLook();
        FollowTarget();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        // Mouse X rotates left/right.
        yaw += mouseX;

        // Mouse Y rotates up/down.
        // Subtracting gives the usual "move mouse up = look up" behaviour.
        pitch -= mouseY;

        // Prevents the camera from flipping too far up/down.
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void FollowTarget()
    {
        if (useSmoothing)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target.position,
                followSmoothness * Time.deltaTime
            );
        }
        else
        {
            transform.position = target.position;
        }
    }

    /// <summary>
    /// Returns the camera rig's forward direction flattened onto the ground.
    /// This is used by the player to face/move in the same direction as the camera.
    /// </summary>
    public Vector3 GetFlatForwardDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        return forward.normalized;
    }

    /// <summary>
    /// Returns the camera rig's right direction flattened onto the ground.
    /// This is used for strafing left/right relative to the camera.
    /// </summary>
    public Vector3 GetFlatRightDirection()
    {
        Vector3 right = transform.right;
        right.y = 0f;

        return right.normalized;
    }
}