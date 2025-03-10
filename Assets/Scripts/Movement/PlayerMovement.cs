using UnityEngine;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    private PlayerStats playerStats;

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

    private float movementSpeed;
    private float targetMovementSpeed;
    private Vector3 velocity;
    private float baseYPosition;
    private Animator fishAnimator;
    private bool inWater; // maybe use later for animation purposes
    Transform cam;
    private Rigidbody rb;
    private bool canPitchUp = true;

    private Vector3 currentRandomOffset;
    private float currentRandomSpeed;
    private float randomUpdateInterval = 1f;  // Update random values every second
    private float lastRandomUpdateTime;

    [Header("AI Settings")]
    private float sprintStartEnergyThreshold = 40f;  // Need this much energy to start sprinting
    private float sprintStopEnergyThreshold = 10f;   // Stop sprinting when energy drops below this
    private bool isAISprinting = false;              // Track sprint state

    private void Start()
    {
        cam = Camera.main.transform;
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        inWater = true;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;

        // Get the player stats component
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = gameObject.AddComponent<PlayerStats>();
        }

        // Subscribe to player death event
        playerStats.OnPlayerDeath += OnPlayerDeath;

        // Sync UI with initial player stats
        if (uiManager != null && playerStats.IsCurrentPlayer)
        {
            uiManager.SetHealth(playerStats.CurrentHealth);
            uiManager.SetEnergy(playerStats.CurrentEnergy);
        }

        UpdateRandomValues();  // Initialize random values
    }

    private void UpdateRandomValues()
    {
        currentRandomOffset = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );
        currentRandomSpeed = maxForwardSpeed * UnityEngine.Random.Range(0.8f, 1.2f);
        lastRandomUpdateTime = Time.time;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= OnPlayerDeath;
        }
    }

    private void OnPlayerDeath()
    {
        // Handle player death logic
        if (playerStats.IsCurrentPlayer && uiManager != null)
        {
            uiManager.DecreaseLives();
        }
    }

    void FixedUpdate()
    {
        Vector3 movement;
        // Only control if this is the current player
        if (!playerStats.IsCurrentPlayer)
        {
            // Check if it's time to update random values
            if (Time.time - lastRandomUpdateTime >= randomUpdateInterval)
            {
                UpdateRandomValues();
            }

            GameObject currentPlayer = GameController.currentPlayer;
            if (currentPlayer != null)
            {
                Vector3 directionToPlayer = currentPlayer.transform.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;
                float minDistance = 5f; // Minimum distance to maintain from player
                float targetSpeed = maxForwardSpeed;

                if (distanceToPlayer > minDistance)
                {
                    // Sprint to catch up if too far behind with energy management
                    if (distanceToPlayer > 12f)
                    {
                        if (!isAISprinting && playerStats.CurrentEnergy >= sprintStartEnergyThreshold)
                        {
                            isAISprinting = true;
                        }
                        else if (isAISprinting && playerStats.CurrentEnergy <= sprintStopEnergyThreshold)
                        {
                            isAISprinting = false;
                        }

                        if (isAISprinting)
                        {
                            targetSpeed = maxForwardSpeed * 2f;
                            float energyCost = 40f * Time.fixedDeltaTime;
                            playerStats.TryUseEnergy(energyCost);
                        }
                    }
                    else
                    {
                        isAISprinting = false;
                    }

                    Vector3 normalizedDirection = (directionToPlayer + currentRandomOffset).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

                    movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, baseAcceleration * Time.fixedDeltaTime);
                } // are we sure we want the fish to be able to die offscren, yeah like e
                //they can't die from sprinting
                else
                {
                    movementSpeed = Mathf.MoveTowards(movementSpeed, 0f, baseDeceleration * Time.fixedDeltaTime);
                }
                movement = transform.forward * movementSpeed;
            }
            else
            {
                movement = Vector3.zero;
            }
        }


        else
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

                    float energyCost = 40f * Time.fixedDeltaTime;
                    if (!playerStats.TryUseEnergy(energyCost))
                    {
                        if (Time.time - lastSprintDamageTime >= sprintDamageInterval)
                        {
                            playerStats.ModifyHealth(-sprintDamageAmount);
                            lastSprintDamageTime = Time.time;
                        }
                    }

                    // Update UI for current player
                    if (uiManager != null)
                    {
                        uiManager.SetEnergy(playerStats.CurrentEnergy);
                        uiManager.SetHealth(playerStats.CurrentHealth);
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
            movement = transform.forward * movementSpeed;
        }

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
            float energyCost = waterExitEnergyCost;
            if (playerStats.TryUseEnergy(energyCost))
            {
                // Successfully used energy
            }
            else
            {
                // Not enough energy, consume all remaining energy and damage health
                float remainingEnergy = playerStats.CurrentEnergy;
                playerStats.SetEnergy(0);
                float healthDamage = (energyCost - remainingEnergy) * 0.5f;
                playerStats.ModifyHealth(-healthDamage);
            }

            // Update UI for current player
            if (uiManager != null && playerStats.IsCurrentPlayer)
            {
                uiManager.SetEnergy(playerStats.CurrentEnergy);
                uiManager.SetHealth(playerStats.CurrentHealth);
            }
        }
    }
}