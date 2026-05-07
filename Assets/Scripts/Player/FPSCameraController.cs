using UnityEngine;

/// <summary>
/// Simple first-person camera controller.
/// 
/// Mouse X rotates the whole player body left/right.
/// Mouse Y rotates only the camera up/down, with pitch clamping.
/// 
/// This keeps shooting simple because the camera's centre point becomes
/// the player's aim direction.
/// </summary>
public class FPSCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player object/body that should rotate left and right.")]
    public Transform playerBody;

    [Header("Mouse Look")]
    public float mouseSensitivityX = 120f;
    public float mouseSensitivityY = 100f;

    [Header("Vertical Look Clamp")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        // Rotate the player body horizontally.
        playerBody.Rotate(Vector3.up * mouseX);

        // Rotate the camera vertically.
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}