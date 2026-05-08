using UnityEngine;

/// <summary>
/// Handles player shooting using a SphereCast.
/// 
/// A SphereCast works like a raycast with thickness, making hits easier to detect.
/// It also gives us useful data for tracking shots fired, shots hit, and accuracy.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Where shots come from. Mostly used later for visual effects.")]
    public Transform firePoint;

    [Tooltip("Camera used to aim from the centre of the screen.")]
    public Camera playerCamera;

    [Header("Shooting")]
    public float fireRate = 0.25f;
    public float shotRange = 50f;
    public float shotRadius = 0.35f;
    public float damage = 25f;

    [Header("Debug")]
    public bool drawDebugRay = true;
    public bool logCoverShootDebug = true;

    private float nextFireTime;

    private SectionInstance currentSection;
    private PlayerCoverController playerCoverController;

    private void Awake()
    {
        playerCoverController = GetComponent<PlayerCoverController>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (!CanShoot())
            {
                if (logCoverShootDebug)
                {
                    Debug.Log("Shoot blocked. InCover=" + (playerCoverController != null && playerCoverController.IsInCover) + ", Peeking=" + (playerCoverController != null && playerCoverController.IsPeekingFromCover));
                }

                return;
            }

            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    public void SetCurrentSection(SectionInstance section)
    {
        currentSection = section;
    }

    private bool CanShoot()
    {
        if (playerCoverController == null)
        {
            return true;
        }

        if (!playerCoverController.IsInCover)
        {
            return true;
        }

        return playerCoverController.IsPeekingFromCover;
    }

    private void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerShooter is missing a camera reference.");
            return;
        }

        if (currentSection != null)
        {
            currentSection.RegisterShotFired();
        }

        Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (drawDebugRay)
        {
            Debug.DrawRay(aimRay.origin, aimRay.direction * shotRange, Color.red, 0.2f);
        }

        RaycastHit hit;

        bool hasHit = Physics.SphereCast(
            aimRay,
            shotRadius,
            out hit,
            shotRange
        );

        if (!hasHit)
        {
            return;
        }

        EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();

        if (enemyHealth != null)
        {
            if (currentSection != null)
            {
                currentSection.RegisterShotHit();
            }

            enemyHealth.TakeDamage(damage);
        }
    }
}