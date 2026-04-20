using UnityEngine;

/// <summary>
/// Handles enemy AI movement. The enemy wanders randomly within the map at slow speed,
/// changing direction at random intervals. Each enemy has a detection radius that will
/// later be used to spot the player and trigger alternate behavior (chase, attack, etc).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    [Tooltip("Slow wandering speed in units per second.")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("How quickly the enemy accelerates toward its chosen direction.")]
    [SerializeField] private float acceleration = 10f;

    [Tooltip("How smoothly the enemy turns to face the new direction (higher = snappier).")]
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Direction Change Interval")]
    [Tooltip("Minimum seconds before picking a new random direction.")]
    [SerializeField] private float minDirectionChangeTime = 2f;

    [Tooltip("Maximum seconds before picking a new random direction.")]
    [SerializeField] private float maxDirectionChangeTime = 5f;

    [Header("Map Bounds")]
    [Tooltip("Center of the map (usually world origin).")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero;

    [Tooltip("Half-size of the map on X and Z axes. A value of 10 = 20x20 playable area.")]
    [SerializeField] private Vector2 mapHalfExtents = new Vector2(10f, 10f);

    [Tooltip("Distance from map edge at which the enemy starts turning back inward.")]
    [SerializeField] private float boundaryBuffer = 1.5f;

    [Header("Detection")]
    [Tooltip("Radius within which the enemy can detect the player.")]
    [SerializeField] private float detectionRadius = 4f;

    [Tooltip("Layer mask for objects the enemy should detect (set to Player layer).")]
    [SerializeField] private LayerMask detectionLayerMask;

    [Header("Debug Gizmos")]
    [Tooltip("Shows the detection radius in the Scene view.")]
    [SerializeField] private bool showDetectionGizmo = true;

    [Tooltip("Shows the current movement direction in the Scene view.")]
    [SerializeField] private bool showDirectionGizmo = true;

    [Tooltip("Shows the map boundary in the Scene view.")]
    [SerializeField] private bool showMapBoundsGizmo = true;

    #endregion

    #region Private Fields

    // Cached Rigidbody — guaranteed by RequireComponent.
    private Rigidbody rb;

    // Current normalized direction the enemy is moving toward.
    private Vector3 currentDirection;

    // Timer tracking when to pick the next random direction.
    private float nextDirectionChangeTime;

    // True when the player is currently inside the detection radius.
    private bool playerDetected = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Cache Rigidbody — guaranteed via RequireComponent.
        rb = GetComponent<Rigidbody>();

        // Top-down movement — no gravity, no tumbling on collisions.
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        // Pick a starting direction immediately so the enemy doesn't stand still.
        PickNewRandomDirection();
    }

    private void Update()
    {
        // Detection runs every frame for responsiveness — cheap operation.
        CheckForPlayerInDetectionRadius();

        // Handle the interval timer for changing direction.
        HandleDirectionChangeTimer();
    }

    private void FixedUpdate()
    {
        // All physics movement happens in FixedUpdate for consistency.
        ApplyMovement();

        // Boundary enforcement must run in physics loop since it adjusts direction.
        EnforceMapBoundary();
    }

    #endregion

    #region Direction Logic

    /// <summary>
    /// Counts down to the next direction change and picks a new random direction when the timer expires.
    /// </summary>
    private void HandleDirectionChangeTimer()
    {
        if (Time.time >= nextDirectionChangeTime)
        {
            PickNewRandomDirection();
        }
    }

    /// <summary>
    /// Picks a new random direction on the XZ plane and schedules the next change.
    /// </summary>
    private void PickNewRandomDirection()
    {
        // Random angle in degrees around the full circle.
        float randomAngle = Random.Range(0f, 360f);

        // Convert angle to a direction vector on the XZ plane.
        float angleRad = randomAngle * Mathf.Deg2Rad;
        currentDirection = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)).normalized;

        // Schedule the next direction change at a random interval.
        float randomInterval = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
        nextDirectionChangeTime = Time.time + randomInterval;
    }

    #endregion

    #region Movement Logic

    /// <summary>
    /// Smoothly accelerates the Rigidbody toward the current direction at wandering speed.
    /// Also rotates the enemy to face the movement direction.
    /// </summary>
    private void ApplyMovement()
    {
        // Target velocity = direction * speed, locked to the XZ plane.
        Vector3 targetVelocity = currentDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve Y if gravity re-enabled later.

        // Smoothly interpolate current velocity toward the target.
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = newVelocity;

        // Rotate smoothly to face the movement direction.
        if (currentDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }

    /// <summary>
    /// Keeps the enemy inside the map bounds by flipping direction inward when it nears an edge.
    /// Prevents the enemy from walking off the playable area.
    /// </summary>
    private void EnforceMapBoundary()
    {
        Vector3 pos = transform.position;
        Vector3 localPos = pos - mapCenter;

        bool needsRedirect = false;
        Vector3 newDirection = currentDirection;

        // Check X-axis bounds with buffer — flip X direction if too close to edge.
        if (localPos.x > mapHalfExtents.x - boundaryBuffer && currentDirection.x > 0f)
        {
            newDirection.x = -Mathf.Abs(currentDirection.x);
            needsRedirect = true;
        }
        else if (localPos.x < -mapHalfExtents.x + boundaryBuffer && currentDirection.x < 0f)
        {
            newDirection.x = Mathf.Abs(currentDirection.x);
            needsRedirect = true;
        }

        // Check Z-axis bounds with buffer — flip Z direction if too close to edge.
        if (localPos.z > mapHalfExtents.y - boundaryBuffer && currentDirection.z > 0f)
        {
            newDirection.z = -Mathf.Abs(currentDirection.z);
            needsRedirect = true;
        }
        else if (localPos.z < -mapHalfExtents.y + boundaryBuffer && currentDirection.z < 0f)
        {
            newDirection.z = Mathf.Abs(currentDirection.z);
            needsRedirect = true;
        }

        if (needsRedirect)
        {
            currentDirection = newDirection.normalized;
        }
    }

    #endregion

    #region Detection Logic

    /// <summary>
    /// Checks whether any collider on the detection layer is within the detection radius.
    /// Sets the playerDetected flag — useful for future chase/attack behavior hooks.
    /// </summary>
    private void CheckForPlayerInDetectionRadius()
    {
        // OverlapSphere returns all colliders within the radius on the given layer.
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayerMask);

        playerDetected = hits.Length > 0;

        // Hook for future expansion — trigger alert state, chase player, etc.
        // if (playerDetected) { ... }
    }

    /// <summary>
    /// Public accessor so other scripts (GameManager, UI) can query detection state.
    /// </summary>
    public bool IsPlayerDetected() => playerDetected;

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        // Detection radius — red when player is detected, yellow otherwise.
        if (showDetectionGizmo)
        {
            Gizmos.color = playerDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // Current movement direction — green arrow in front of enemy.
        if (showDirectionGizmo && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, currentDirection * 2f);
        }

        // Map boundary — cyan rectangle showing the walkable area.
        if (showMapBoundsGizmo)
        {
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(mapHalfExtents.x * 2f, 0.1f, mapHalfExtents.y * 2f);
            Gizmos.DrawWireCube(mapCenter, size);
        }
    }

    #endregion
}