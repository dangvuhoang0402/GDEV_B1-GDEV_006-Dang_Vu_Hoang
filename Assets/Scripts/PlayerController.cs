using UnityEngine;
public class PlayerController : MonoBehaviour
{   
    private float moveSpeed = 5f;
    private float acceleration = 50f;
    private float deceleration = 60f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
    private Rigidbody rb;
    private Vector2 moveInput;


    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }
    private void ReadInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical);
    }
    private void ApplyMovement()
    {
        Vector3 targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );
        rb.linearVelocity = newVelocity;
    }
}