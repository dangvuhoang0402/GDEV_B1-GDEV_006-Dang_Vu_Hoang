using UnityEngine;

/// <summary>
/// Handles player shooting. Space key fires a bullet in the player's forward direction.
/// Enforces a fire rate cooldown between shots using Time.time tracking.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerShooting : MonoBehaviour
{
    #region Inspector Fields

    [Header("Shooting Settings")]
    [Tooltip("Bullet prefab to spawn when firing. Must have a Bullet component attached.")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Transform marking where bullets spawn. Usually placed in front of the player.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Shots per second. Higher = faster firing rate.")]
    [SerializeField] private float fireRate = 3f;

    [Tooltip("Velocity applied to each bullet at spawn time.")]
    [SerializeField] private float bulletSpeed = 20f;

    [Tooltip("Lifetime of each bullet in seconds before auto-destruction.")]
    [SerializeField] private float bulletLifetime = 3f;

    [Header("Debug")]
    [Tooltip("Draws a ray in Scene view showing the fire direction.")]
    [SerializeField] private bool showFireDirectionGizmo = true;

    #endregion

    #region Private Fields

    // Time.time value when the next shot becomes available.
    private float nextFireTime = 0f;

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        // Input polling stays in Update for maximum responsiveness.
        HandleShootingInput();
    }

    #endregion

    #region Shooting Logic

    /// <summary>
    /// Checks for the Space key and fires a bullet if the cooldown has elapsed.
    /// </summary>
    private void HandleShootingInput()
    {
        // GetKey = hold to auto-fire. Use GetKeyDown if you want single shots per press.
        if (!Input.GetKey(KeyCode.Space))
            return;

        // Fire rate gate — block until cooldown expires.
        if (Time.time < nextFireTime)
            return;

        FireBullet();

        // Schedule the next allowed fire time based on fire rate.
        // Example: fireRate = 3 means 1/3 = 0.33s between shots.
        nextFireTime = Time.time + (1f / fireRate);
    }

    /// <summary>
    /// Instantiates a bullet at the fire point and launches it in the player's forward direction.
    /// </summary>
    private void FireBullet()
    {
        // Safety check — avoid null reference errors if prefab or fire point is missing.
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("PlayerShooting: Bullet prefab or fire point is not assigned.", this);
            return;
        }

        // Spawn the bullet at the fire point with the player's rotation.
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Apply velocity to the bullet's Rigidbody in the player's forward direction.
        if (bullet.TryGetComponent(out Rigidbody bulletRb))
        {
            // transform.forward uses the player's current facing direction.
            bulletRb.linearVelocity = transform.forward * bulletSpeed;
        }

        // Auto-destroy the bullet after its lifetime expires to prevent scene clutter.
        Destroy(bullet, bulletLifetime);
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Draws a forward ray from the fire point in Scene view for aiming debug.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showFireDirectionGizmo || firePoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(firePoint.position, transform.forward * 2f);
    }

    #endregion
}