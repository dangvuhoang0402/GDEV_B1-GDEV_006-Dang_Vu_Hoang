using UnityEngine;

/// <summary>
/// Handles 3D top-down player movement using Rigidbody physics.
/// Player moves toward a target position set by left mouse click on the map.
/// Clicking a new position redirects movement immediately.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    [Tooltip("Maximum movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How quickly the player accelerates toward target velocity.")]
    [SerializeField] private float acceleration = 50f;

    [Tooltip("How quickly the player decelerates when reaching destination.")]
    [SerializeField] private float deceleration = 60f;

    [Tooltip("Distance threshold to consider the destination reached.")]
    [SerializeField] private float stoppingDistance = 0.15f;

    [Header("Click Settings")]
    [Tooltip("Layer mask for raycast — set this to your Ground layer in the Inspector.")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("Camera used to cast the click ray. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera playerCamera;

    [Header("Debug")]
    [Tooltip("Draws a gizmo sphere at the current click target in the Scene view.")]
    [SerializeField] private bool showDestinationGizmo = true;

    #endregion

    #region Private Fields

    // Cached Rigidbody component — guaranteed by RequireComponent.
    private Rigidbody rb;

    // World-space position the player is currently moving toward.
    private Vector3 targetDestination;

    // Whether the player currently has an active destination to move to.
    private bool hasDestination = false;

    // Tracks if the player has physically arrived at the destination this frame.
    private bool isMoving = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Cache Rigidbody — guaranteed to exist via RequireComponent.
        rb = GetComponent<Rigidbody>();

        // Top-down movement lives on the XZ plane — no gravity needed.
        rb.useGravity = false;

        // Prevent physics from rotating the capsule on collisions.
        rb.freezeRotation = true;

        // Continuous detection prevents tunneling at higher speeds.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Interpolation smooths visual position between fixed physics steps.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Fall back to Camera.main if no camera was assigned in the Inspector.
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Initialize destination to the player's starting position.
        targetDestination = transform.position;
    }

    private void Update()
    {
        // Poll input every frame for maximum click responsiveness.
        HandleClickInput();
    }

    private void FixedUpdate()
    {
        // All physics movement runs in FixedUpdate for frame-rate independence.
        MoveTowardDestination();
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Casts a ray from the camera through the mouse position onto the ground plane.
    /// On left click, sets the hit point as the new movement destination immediately.
    /// </summary>
    private void HandleClickInput()
    {
        if (!Input.GetMouseButton(0))
            return;

        // Build a ray from the camera through the mouse cursor into the world.
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast against the ground layer only — ignores players, enemies, UI, etc.
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            SetDestination(hit.point);
        }
    }

    /// <summary>
    /// Sets a new movement destination, immediately overriding any previous target.
    /// Keeps Y axis locked to the player's current height to stay on the XZ plane.
    /// </summary>
    /// <param name="worldPosition">World-space point clicked on the ground.</param>
    private void SetDestination(Vector3 worldPosition)
    {
        // Lock Y to current player height — prevents vertical drift on uneven ground.
        targetDestination = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
        hasDestination = true;
        isMoving = true;
    }

    #endregion

    #region Movement Logic

    /// <summary>
    /// Accelerates the Rigidbody toward the target destination using smooth
    /// velocity interpolation. Decelerates and stops once within stopping distance.
    /// </summary>
    private void MoveTowardDestination()
    {
        if (!hasDestination)
        {
            BrakeToStop();
            return;
        }

        Vector3 toDestination = targetDestination - transform.position;

        // Ignore Y axis difference — movement is purely on the XZ plane.
        toDestination.y = 0f;

        float distanceToTarget = toDestination.magnitude;

        // Check if we've arrived within the stopping threshold.
        if (distanceToTarget <= stoppingDistance)
        {
            BrakeToStop();
            hasDestination = false;
            isMoving = false;
            return;
        }

        // Calculate desired velocity — full speed in the direction of destination.
        Vector3 desiredVelocity = toDestination.normalized * moveSpeed;
        desiredVelocity.y = rb.linearVelocity.y; // Preserve Y in case gravity re-enabled later.

        // Smoothly accelerate toward desired velocity using MoveTowards.
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            desiredVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = newVelocity;

        // Rotate the player to face the movement direction smoothly.
        FaceMovementDirection(toDestination.normalized);
    }

    /// <summary>
    /// Smoothly decelerates the Rigidbody to a full stop.
    /// </summary>
    private void BrakeToStop()
    {
        Vector3 brakingVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            Vector3.zero,
            deceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = brakingVelocity;
    }

    /// <summary>
    /// Instantly rotates the player to face the direction of movement.
    /// Replace Quaternion.LookRotation with Slerp for a smoother turning feel.
    /// </summary>
    /// <param name="direction">Normalized XZ direction vector toward destination.</param>
    private void FaceMovementDirection(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        // LookRotation builds a rotation where forward = direction, up = world up.
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = targetRotation;
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Draws the destination point in the Scene view for easier debugging.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDestinationGizmo || !hasDestination)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(targetDestination, 0.2f);
        Gizmos.DrawLine(transform.position, targetDestination);
    }

    #endregion
}