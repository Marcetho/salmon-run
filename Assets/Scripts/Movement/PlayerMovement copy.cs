using UnityEngine;

public class PlayerMovementCopy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;

    [Header("Movement Settings")]
    public float maxForwardSpeed = 5f;
    public float maxBackwardSpeed = -0.5f;
    public float baseAcceleration = 4f;
    public float baseDeceleration = 16f;
    public float rotationSpeed = 100f;
    public float yawAmount = 5f;
    public float pitchAmount = 45f;  // For up/down tilt
    private ConstantForce eForce; // external force (river current, gravity, water buoyancy)
    private Vector3 eForceDir; // net direction of external force

    [Header("Energy Settings")]
    [SerializeField] private float sprintDamageInterval = 0.5f;
    [SerializeField] private float sprintDamageAmount = 5f;
    [SerializeField] private float waterExitEnergyCost = 40f;

    private float lastSprintDamageTime;

    private float movementSpeed;  // renamed from currentSpeed
    private float targetMovementSpeed;  // renamed from targetSpeed
    private Vector3 velocity;
    private float baseYPosition;
    private Animator fishAnimator;
    private bool inWater; // maybe use later for animation purposes
    Transform cam;
    private Rigidbody rb;
    private bool isJumping = false;
    private bool isExitingWater = false;
    private bool canTiltUp = true;

    private void Start()
    {
        cam = Camera.main.transform;
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        inWater = true;  // Ensure inWater is always true
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rotationSpeed = 100f;
        maxForwardSpeed = 5f;
        eForceDir = new Vector3(0, 0, 0);
    }
    void FixedUpdate()
    {
        float yawInput = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);

        // Remove pitch input logic
        float pitchInput = 0f;

        // Apply rotations only if forward velocity is not zero
        if (movementSpeed != 0)
        {
            transform.Rotate(Vector3.up, yawInput * rotationSpeed * Time.deltaTime);

            // Calculate and apply tilt
            float yaw = yawInput * yawAmount;
            float pitch = pitchInput * pitchAmount;
            Quaternion targetRotation = Quaternion.Euler(pitch, transform.eulerAngles.y, yaw);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }

        // Handle speed changes based on shift/ctrl
        float moveInput = Input.GetKey(KeyCode.W) ? 1f : 0f;  // "W" moves forward, "S" does nothing
        float speed = maxForwardSpeed;
        Debug.Log("Speed: " + speed + " Max Forward Speed: " + maxForwardSpeed);
        float acceleration = baseAcceleration;
        float deceleration = baseDeceleration;
        if (Input.GetKey(KeyCode.LeftShift) && moveInput > 0) //sprint
        {
            speed = maxForwardSpeed * 2.5f;
            acceleration *= 60f;

            if (!uiManager.HasEnoughEnergy(40f * Time.fixedDeltaTime))
            {
                if (Time.time - lastSprintDamageTime >= sprintDamageInterval)
                {
                    uiManager.DecreaseHealth(sprintDamageAmount);
                    lastSprintDamageTime = Time.time;
                }
            }
            else
            {
                uiManager.DecreaseEnergy(40f * Time.fixedDeltaTime);
            }
        }

        targetMovementSpeed = moveInput * speed;
        if (speed > targetMovementSpeed)
            movementSpeed = Mathf.MoveTowards(movementSpeed, targetMovementSpeed, deceleration * Time.fixedDeltaTime);
        else
            movementSpeed = Mathf.MoveTowards(movementSpeed, targetMovementSpeed, acceleration * Time.fixedDeltaTime);
        if (movementSpeed < maxBackwardSpeed) movementSpeed = maxBackwardSpeed;

        // Apply movement in the direction the fish is facing
        Vector3 movement = transform.forward * movementSpeed;

        rb.AddForce(movement);

        eForce.force = eForceDir;

        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", movementSpeed);
        }
    }
}