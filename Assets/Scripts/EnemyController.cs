using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private float speed_Normal = 2f;
    private float acceleration = 50f;
    private float deceleration = 60f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation=true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private Rigidbody rb;
    private Vector2 moveInput;

    void Update()
    {

    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void ApplyMovement(){
        Vector3 targetVelocity= new Vector3(1f,0f,-1f)*speed_Normal;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        Vector3 newVelocity = Vector3.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );
        rb.linearVelocity= newVelocity;
    }
}
