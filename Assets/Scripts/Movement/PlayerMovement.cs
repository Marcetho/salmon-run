using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxForwardSpeed = 5f;
    public float maxBackwardSpeed = -0.5f;
    public float acceleration = 2f;
    public float deceleration = 2f;
    public float rotationSpeed = 100f;
    public float yawAmount = 5f;
    public float pitchAmount = 45f;  // For up/down tilt
    private ConstantForce eForce; // external force (river current, gravity, water buoyancy)
    private Vector3 eForceDir; // net direction of external force

    private float currentSpeed;
    private float targetSpeed;
    private Vector3 velocity;
    private float baseYPosition;
    private Animator fishAnimator;
    private bool inWater; // maybe use later for animation purposes
    Transform cam;
    private Rigidbody rb;

    private void Start()
    {
        cam = Camera.main.transform;
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        inWater = true;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // Handle rotation and tilt input
        float yawInput = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        if (Mathf.Abs(currentSpeed) < 0.1f) yawInput = 0f;
        float pitchInput = Input.GetKey(KeyCode.S) ? 1f : (Input.GetKey(KeyCode.W) ? -1f : 0f);

        // Apply rotations
        transform.Rotate(Vector3.up, yawInput * rotationSpeed * Time.deltaTime);

        // Calculate and apply tilt
        float yaw = yawInput * yawAmount;
        float pitch = pitchInput * pitchAmount;
        Quaternion targetRotation = Quaternion.Euler(pitch, transform.eulerAngles.y, yaw);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);

        // Handle speed changes based on shift/ctrl
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.LeftShift)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftControl)) moveInput = -0.3f;

        targetSpeed = moveInput * maxForwardSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        if (currentSpeed < maxBackwardSpeed) currentSpeed = maxBackwardSpeed;

        // Apply movement in the direction the fish is facing
        Vector3 movement = transform.forward * currentSpeed;

        // float waveMotion = Mathf.Sin(Time.time * 2f) * 0.002f;
        // movement += transform.up * waveMotion;

        rb.AddForce(movement);
        eForce.force = eForceDir;

        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", currentSpeed);
        }
    }

    private void OnTriggerEnter(Collider other) { //in water
        if (other.gameObject.tag == "Water")
            inWater = true;
            rb.linearDamping = 1f;
            rotationSpeed = 100f;
            maxForwardSpeed = 5f;
            eForceDir = new Vector3(0, 0, 0);
    }

    private void OnTriggerExit(Collider other){ //out of water
        if (other.gameObject.tag == "Water")
            inWater = false;
            rb.linearDamping = 0.1f;
            rotationSpeed = 0f;
            maxForwardSpeed = 0.1f;
            eForceDir = new Vector3(0, -3, 0);
    }
}