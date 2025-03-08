using UnityEngine;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;
    private PlayerStats playerStats;
    private GameController gameController;

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
    [SerializeField] private float waterExitEnergyCost = 20f;

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
    [SerializeField] private float followSpeed = 5f; // Speed to follow player
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations
    [SerializeField] private float followDistance = 2f; // Base distance behind player
    [SerializeField] private float waterSurfaceYPosition = 0f; // Y position of water surface
    [SerializeField] private float independentMovementStrength = 0.5f; // How much fish move on their own
    [SerializeField] private float forwardBiasStrength = 0.7f; // Tendency to swim forward
    [SerializeField] private float naturalMovementSpeed = 0.8f; // Base speed for natural movement
    [SerializeField] private float positionUpdateInterval = 3f; // How often to update natural position

    // For rotation smoothing
    private Vector3 currentRotationVelocity;
    private Quaternion targetRotation;

    // For fixed positioning around player
    private Vector3 relativeOffset = Vector3.zero;
    private float uniqueAngle = 0f;

    // For natural movement
    private Vector3 naturalMovementVector;
    private float lastNaturalUpdateTime;
    private float independentSpeedFactor;
    private bool preferFrontPosition = false;

    private void Start()
    {
        cam = Camera.main.transform;
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        inWater = true;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;

        // Find game controller
        gameController = FindFirstObjectByType<GameController>();

        // Get the player stats component
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = gameObject.AddComponent<PlayerStats>();
        }

        // Subscribe to player death event - only if this is the player-controlled fish
        playerStats.OnPlayerDeath += OnPlayerDeath;

        // Subscribe to AI fish death event
        playerStats.OnAIFishDeath += OnAIFishDeath;

        // Sync UI with initial player stats
        if (uiManager != null && playerStats.IsCurrentPlayer)
        {
            uiManager.SetHealth(playerStats.CurrentHealth);
            uiManager.SetEnergy(playerStats.CurrentEnergy);
        }

        UpdateRandomValues();  // Initialize random values

        // Initialize water surface position if not set
        if (waterSurfaceYPosition == 0f)
        {
            // Try to find water surface from the scene
            GameObject water = GameObject.FindGameObjectWithTag("Water");
            if (water != null)
            {
                waterSurfaceYPosition = water.transform.position.y;
            }
        }

        // Generate a fixed relative position for this AI fish based on its instanceID
        if (!playerStats.IsCurrentPlayer)
        {
            // Use instanceID to create a unique angle for positioning
            int fishID = gameObject.GetInstanceID();
            uniqueAngle = (fishID % 360) * Mathf.Deg2Rad;
            
            // Randomize position preferences
            preferFrontPosition = (fishID % 3 == 0); // About 1/3 of fish prefer to be in front
            
            // Create variation in formation - allow positions all around player, not just behind
            float horizontalRadius = UnityEngine.Random.Range(1.5f, 3.5f);
            float verticalOffset = UnityEngine.Random.Range(-1f, 1f);
            float forwardOffset = preferFrontPosition ? 
                UnityEngine.Random.Range(1f, 3f) : 
                UnityEngine.Random.Range(-3f, 1f); // Some fish in front, some behind
                
            relativeOffset = new Vector3(
                Mathf.Sin(uniqueAngle) * horizontalRadius,
                verticalOffset,
                Mathf.Cos(uniqueAngle) * horizontalRadius + forwardOffset
            );
            
            // Initialize natural movement values
            UpdateNaturalMovement();
            independentSpeedFactor = UnityEngine.Random.Range(0.7f, 1.3f);
            lastNaturalUpdateTime = Time.time;
        }
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

    private void UpdateNaturalMovement()
    {
        // Create random movement vector for independent motion
        naturalMovementVector = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-0.5f, 0.5f), // Less vertical movement
            UnityEngine.Random.Range(-0.5f, 1.5f)  // Bias toward forward movement
        ).normalized * independentMovementStrength;
        
        // Add additional forward bias so fish tend to swim ahead
        if (preferFrontPosition)
        {
            naturalMovementVector += Vector3.forward * forwardBiasStrength;
            naturalMovementVector = naturalMovementVector.normalized * independentMovementStrength;
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= OnPlayerDeath;
            playerStats.OnAIFishDeath -= OnAIFishDeath;
        }
    }

    private void OnPlayerDeath()
    {
        // Handle player death logic only for player-controlled fish
        if (playerStats.IsCurrentPlayer && uiManager != null)
        {
            uiManager.DecreaseLives();
        }
    }

    private void OnAIFishDeath(GameObject deadFish)
    {
        // Notify GameController about AI fish death
        if (gameController != null)
        {
            gameController.OnAIFishDied(deadFish);
        }
    }

    void FixedUpdate()
    {
        Vector3 movement;

        // Only control if this is the current player
        if (!playerStats.IsCurrentPlayer)
        {
            GameObject currentPlayer = GameController.currentPlayer;
            if (currentPlayer != null)
            {
                // Update natural movement periodically
                if (Time.time - lastNaturalUpdateTime > positionUpdateInterval)
                {
                    UpdateNaturalMovement();
                    lastNaturalUpdateTime = Time.time;
                }
                
                // Get player transform and movement data
                Transform playerTransform = currentPlayer.transform;
                PlayerMovement playerMovement = currentPlayer.GetComponent<PlayerMovement>();
                float playerSpeed = playerMovement != null ? Mathf.Abs(playerMovement.movementSpeed) : 5f;
                
                // Calculate base target position relative to player
                Vector3 baseTargetPosition = playerTransform.TransformPoint(relativeOffset);
                
                // Add natural movement to create independent behavior
                Vector3 naturalOffset = playerTransform.TransformDirection(naturalMovementVector);
                Vector3 targetPosition = baseTargetPosition + naturalOffset;
                
                // Ensure fish doesn't jump out of water
                float minDepth = waterSurfaceYPosition - 0.5f;
                if (targetPosition.y > minDepth)
                {
                    targetPosition.y = minDepth;
                }
                
                // Calculate direction to target position
                Vector3 directionToTarget = targetPosition - transform.position;
                float distanceToTarget = directionToTarget.magnitude;
                
                // Calculate a natural-looking rotation that blends target direction and own forward
                Vector3 blendedDirection = Vector3.Slerp(
                    transform.forward,
                    directionToTarget.normalized,
                    0.6f // Smoothly turn toward target
                );
                
                if (blendedDirection.magnitude > 0.01f)
                {
                    targetRotation = Quaternion.LookRotation(blendedDirection);
                    
                    // Add slight random variation to rotation for natural movement
                    float randomYaw = Mathf.Sin(Time.time * 0.8f + uniqueAngle * 10) * 5f;
                    float randomPitch = Mathf.Sin(Time.time * 0.6f + uniqueAngle * 5) * 3f;
                    targetRotation *= Quaternion.Euler(randomPitch, randomYaw, 0);
                    
                    // Apply smoothed rotation
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.fixedDeltaTime * (10f / rotationSmoothTime)
                    );
                }
                
                // Set base movement speed - always moving somewhat forward on their own
                float baseSpeed = naturalMovementSpeed * independentSpeedFactor;
                
                // Adjust speed based on distance to target
                float targetSpeed = baseSpeed;
                if (distanceToTarget > 5f)
                {
                    // Catch up if too far away
                    targetSpeed = playerSpeed * 1.5f;
                }
                else if (distanceToTarget > 2f)
                {
                    // Match speed with slight increase if behind
                    targetSpeed = Mathf.Max(baseSpeed, playerSpeed * 0.9f);
                }
                
                // Independent fish can occasionally sprint on their own
                if (preferFrontPosition && Mathf.Sin(Time.time * 0.2f + uniqueAngle * 8) > 0.7f)
                {
                    targetSpeed *= 1.5f;
                }
                
                // Smooth acceleration to target speed
                movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, baseAcceleration * 1.5f * Time.fixedDeltaTime);
                
                // Apply movement in the direction the fish is facing
                // Independent fish always move somewhat forward on their own
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

                    float energyCost = 20f * Time.fixedDeltaTime;
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

            // Only apply energy cost/damage to player-controlled fish
            if (playerStats.IsCurrentPlayer)
            {
                // Handle energy cost for exiting water
                float energyCost = waterExitEnergyCost;
                if (!playerStats.TryUseEnergy(energyCost))
                {
                    // Not enough energy, consume all remaining energy and damage health
                    float remainingEnergy = playerStats.CurrentEnergy;
                    playerStats.SetEnergy(0);
                    float healthDamage = (energyCost - remainingEnergy) * 0.5f;
                    playerStats.ModifyHealth(-healthDamage);
                }

                // Update UI for current player
                if (uiManager != null)
                {
                    uiManager.SetEnergy(playerStats.CurrentEnergy);
                    uiManager.SetHealth(playerStats.CurrentHealth);
                }
            }
            else
            {
                // For AI fish, reduce energy but don't let them die from jumping
                float aiEnergyCost = waterExitEnergyCost * 0.5f; // Reduced energy cost

                // Make sure AI fish don't drain energy below minimum threshold
                float minEnergyThreshold = 15f;
                float newEnergy = Mathf.Max(minEnergyThreshold, playerStats.CurrentEnergy - aiEnergyCost);
                playerStats.SetEnergy(newEnergy);

                // AI fish will never take health damage from jumping
            }
        }
    }
}