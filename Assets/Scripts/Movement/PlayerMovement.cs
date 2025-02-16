using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxForwardSpeed = 5f;
    public float maxBackwardSpeed = -0.5f;
    public float acceleration = 2f;
    public float deceleration = 2f;
    public float rotationSpeed = 100f;
    public float tiltAmount = 5f;
    public float verticalTiltAmount = 10f;

    private float currentSpeed;
    private float targetSpeed;
    private float baseYPosition;
    private Rigidbody rb;
    private Animator fishAnimator;
    Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        rb.useGravity = false; 
    
    }

    void FixedUpdate()
    {
        // Handle rotation and tilt input
        float rotation = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        if (Mathf.Abs(currentSpeed) < 0.1f) rotation = 0f;
        float verticalInput = Input.GetKey(KeyCode.S) ? 1f : (Input.GetKey(KeyCode.W) ? -1f : 0f);

        // Apply rotations
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.fixedDeltaTime);

        // Calculate and apply tilt
        float sideTilt = rotation * tiltAmount;
        float verticalTilt = verticalInput * verticalTiltAmount;
        Quaternion targetRotation = Quaternion.Euler(verticalTilt, transform.eulerAngles.y, sideTilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);

        // Handle speed changes based on shift/ctrl
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.LeftShift)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftControl)) moveInput = -0.3f;

        targetSpeed = moveInput * maxForwardSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        if (currentSpeed < maxBackwardSpeed) currentSpeed = maxBackwardSpeed;

        // Calculate movement vector
        Vector3 movement = transform.forward * currentSpeed;

        // Add wave motion
        float waveMotion = Mathf.Sin(Time.time * 2f) * 0.002f;
        movement += transform.up * waveMotion;

        // Apply movement using Rigidbody
        rb.linearVelocity = movement;

        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", currentSpeed);
        }
    }
}