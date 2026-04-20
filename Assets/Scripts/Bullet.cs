using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnImpact = true;

    [Header("Visuals")]
    [Tooltip("Color applied to the bullet on spawn.")]
    [SerializeField] private Color bulletColor = Color.yellow;

    private void Awake()
    {
        // Configure rigidbody as before...
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Apply color to the bullet's material at spawn.
        if (TryGetComponent(out Renderer rend))
        {
            // Use material (not sharedMaterial) to avoid modifying the source asset.
            rend.material.color = bulletColor;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (destroyOnImpact)
            Destroy(gameObject);
    }
}