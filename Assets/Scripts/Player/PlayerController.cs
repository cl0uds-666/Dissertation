using UnityEngine;

/// <summary>
/// FPS-style player movement.
/// 
/// The player moves relative to its own forward and right directions.
/// The player's facing direction is controlled by FPSCameraController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private PlayerCoverController playerCoverController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Stops the capsule from falling over.
        rb.freezeRotation = true;

        playerCoverController = GetComponent<PlayerCoverController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forwardMovement = transform.forward * vertical;
        Vector3 sideMovement = transform.right * horizontal;

        moveDirection = (forwardMovement + sideMovement).normalized;
    }

    private void FixedUpdate()
    {
        float movementMultiplier = 1f;

        if (playerCoverController != null)
        {
            movementMultiplier = playerCoverController.GetMovementMultiplier();
        }

        Vector3 newPosition = rb.position + moveDirection * moveSpeed * movementMultiplier * Time.fixedDeltaTime;

        rb.MovePosition(newPosition);
    }
}