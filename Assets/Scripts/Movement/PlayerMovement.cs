using UnityEngine;

public class PlayerMovement : MonoBehaviour
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
    public float pitchAmount = 15f;  // For up/down tilt
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
    private bool canPitchUp = true;

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
        if (Mathf.Abs(movementSpeed) < 0.1f) yawInput = 0f;

        float pitchInput = 0f;
        if (inWater)
        {
            if (Input.GetKey(KeyCode.S))
            {
                pitchInput = 1f;
            }
            else if (Input.GetKey(KeyCode.W) && canPitchUp)
            {
                pitchInput = -1f;
            }

            if (!Input.GetKey(KeyCode.W))
            {
                canPitchUp = true;
            }
        }

        // Force pitch to 0 when out of water
        float pitch = inWater ? (pitchInput * pitchAmount) : 0f;

        // Apply rotations
        transform.Rotate(Vector3.up, yawInput * rotationSpeed * Time.deltaTime);

        // Calculate and apply tilt
        float yaw = yawInput * yawAmount;
        Quaternion targetRotation = Quaternion.Euler(pitch, transform.eulerAngles.y, yaw);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);

        // Handle speed changes based on shift/ctrl
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.Space)) moveInput = 1f;
        if (Input.GetKey(KeyCode.LeftControl)) moveInput = -0.3f;
        float speed = maxForwardSpeed;
        float acceleration = baseAcceleration;
        float deceleration = baseDeceleration;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space)) //sprint
        {
            if (inWater)
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
        }

        targetMovementSpeed = moveInput * speed;
        if (speed > targetMovementSpeed)
            movementSpeed = Mathf.MoveTowards(movementSpeed, targetMovementSpeed, deceleration * Time.fixedDeltaTime);
        else
            movementSpeed = Mathf.MoveTowards(movementSpeed, targetMovementSpeed, acceleration * Time.fixedDeltaTime);
        if (movementSpeed < maxBackwardSpeed) movementSpeed = maxBackwardSpeed;

        // Apply movement in the direction the fish is facing
        Vector3 movement = transform.forward * movementSpeed;

        // float waveMotion = Mathf.Sin(Time.time * 2f) * 0.002f;
        // movement += transform.up * waveMotion;

        if (inWater)
        {
            rb.AddForce(movement);
        }

        eForce.force = eForceDir;

        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", movementSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    { //in water
        if (other.gameObject.CompareTag("Water"))
        {
            inWater = true;
            rb.linearDamping = 1.5f;
            rotationSpeed = 100f;
            maxForwardSpeed = 5f;
            eForceDir = new Vector3(0, 0, 0);
        }
    }

    private void OnTriggerExit(Collider other)
    { //out of water
        if (other.gameObject.CompareTag("Water"))
        {
            inWater = false;
            rb.linearDamping = 0.1f;
            rotationSpeed = 0f;
            maxForwardSpeed = 0.5f;
            eForceDir = new Vector3(0, -3, 0);
            canPitchUp = false;

            // Handle energy cost for exiting water
            float currentEnergy = uiManager.GetCurrentEnergy();
            if (currentEnergy >= waterExitEnergyCost)
            {
                uiManager.DecreaseEnergy(waterExitEnergyCost);
            }
            else
            {
                uiManager.DecreaseEnergy(currentEnergy); // Use remaining energy
                float remainingCost = waterExitEnergyCost - currentEnergy;
                uiManager.DecreaseHealth(remainingCost * 0.5f); // Convert remaining energy cost to health damage
            }
        }
    }
}