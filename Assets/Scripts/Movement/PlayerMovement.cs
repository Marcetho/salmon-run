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
    [SerializeField] private float minDistanceToPlayer = 5f;
    [SerializeField] private float maxDistanceToPlayer = 20f;
    [SerializeField] private float targetPositionUpdateInterval = 3f;  // Reduced from 5f to be more responsive
    [SerializeField] private float minTargetDistance = 3f;
    [SerializeField] private float maxTargetDistance = 15f;
    [SerializeField] private float playerInfluenceFactor = 0.7f; // How much the player influences AI fish movement
    [SerializeField] private float naturalMovementIntensity = 0.2f;  // Reduced from 0.4f for smoother movement
    [SerializeField] private float horizontalSpreadFactor = 8f;      // Controls how wide the school spreads horizontally
    [SerializeField] private float verticalSpreadFactor = 1.5f;      // Reduced from 3f to limit vertical movement
    [SerializeField] private float preferredHeightOffset = 0f;     // Prefer to swim slightly above player height
    [SerializeField] private float rotationSmoothTime = 0.5f;        // Time to smooth rotations
    [SerializeField] private float sprintCatchupMultiplier = 1.3f;   // How much faster AI fish can go when catching up
    private float sprintStartEnergyThreshold = 30f;  // Need this much energy to start sprinting
    private float sprintStopEnergyThreshold = 5f;   // Stop sprinting when energy drops below this
    private bool isAISprinting = false;              // Track sprint state

    // AI target position
    private Vector3 currentAITargetPosition;
    private float lastTargetUpdateTime;
    private bool hasValidTarget = false;

    // For rotation smoothing
    private Vector3 currentRotationVelocity;
    private Quaternion targetRotation;

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
        UpdateAITargetPosition(); // Initialize AI target position
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

    private void UpdateAITargetPosition()
    {
        if (!playerStats.IsCurrentPlayer && GameController.currentPlayer != null)
        {
            Vector3 playerPos = GameController.currentPlayer.transform.position;
            Transform playerTransform = GameController.currentPlayer.transform;

            // Generate a position biased toward the front of the player
            Vector3 forwardDir = playerTransform.forward;

            // Use instanceID for consistent behavior per fish
            int fishID = gameObject.GetInstanceID();
            UnityEngine.Random.InitState(fishID + Mathf.FloorToInt(Time.time));

            // Get player speed from FishSchoolManager to adjust position
            float playerSpeed = 5f; // Default
            bool playerIsSprinting = false;
            if (FishSchoolManager.Instance != null)
            {
                playerSpeed = FishSchoolManager.Instance.GetPlayerSpeed();
                playerIsSprinting = FishSchoolManager.Instance.IsPlayerSprinting();
            }

            // Adjust target distances based on player speed
            float speedFactor = Mathf.Clamp01(playerSpeed / 8f);
            float minTargetDistanceAdjusted = Mathf.Lerp(minTargetDistance * 0.5f, minTargetDistance, speedFactor);
            float maxTargetDistanceAdjusted = Mathf.Lerp(maxTargetDistance * 0.5f, maxTargetDistance, speedFactor);

            // When sprinting, keep fish closer to the front
            if (playerIsSprinting)
            {
                minTargetDistanceAdjusted *= 0.8f;
                maxTargetDistanceAdjusted *= 0.8f;
            }

            // Much wider horizontal spread
            Vector3 rightOffset = playerTransform.right * UnityEngine.Random.Range(-horizontalSpreadFactor, horizontalSpreadFactor);

            // More constrained vertical variation
            float verticalMin = -verticalSpreadFactor * 0.2f;  // Further reduced downward offset
            float verticalMax = verticalSpreadFactor;
            Vector3 upOffset = Vector3.up * (UnityEngine.Random.Range(verticalMin, verticalMax) + preferredHeightOffset);

            // Forward distance based on adjusted ranges
            float forwardDistance = UnityEngine.Random.Range(minTargetDistanceAdjusted, maxTargetDistanceAdjusted);

            // Add slight offset based on fish instanceID for varied positions
            float uniqueAngle = (fishID % 360) * Mathf.Deg2Rad;
            Vector3 uniqueOffset = new Vector3(
                Mathf.Sin(uniqueAngle) * 2f,
                Mathf.Cos(uniqueAngle) * 0.8f,
                0
            );

            // Calculate base position with spread
            currentAITargetPosition = playerPos +
                                    forwardDir * forwardDistance +
                                    rightOffset +
                                    upOffset +
                                    uniqueOffset;

            // Make sure the target is not too far below the player
            float maxDepthDifference = 1.2f;  // Further reduced
            if (currentAITargetPosition.y < playerPos.y - maxDepthDifference)
            {
                currentAITargetPosition.y = playerPos.y - maxDepthDifference;
            }

            // Favor positions above player
            if (currentAITargetPosition.y < playerPos.y)
            {
                // Apply stronger lifting force to targets below player
                currentAITargetPosition.y = Mathf.Lerp(currentAITargetPosition.y, playerPos.y + preferredHeightOffset, 0.4f);
            }

            // Check for obstacles between player and target position
            RaycastHit hit;
            Vector3 dirToTarget = currentAITargetPosition - playerPos;
            if (Physics.Raycast(playerPos, dirToTarget.normalized, out hit, dirToTarget.magnitude,
                              LayerMask.GetMask("Default", "Environment", "Obstacle")))
            {
                // If obstacle detected, adjust target position to be on player's side of obstacle
                float safeDistance = hit.distance * 0.8f; // 80% of distance to obstacle
                currentAITargetPosition = playerPos + dirToTarget.normalized * safeDistance;
            }

            hasValidTarget = true;
            lastTargetUpdateTime = Time.time;
        }
    }

    private bool ShouldUpdateTargetPosition()
    {
        // Update if we don't have a valid target or it's time for an update
        if (!hasValidTarget || Time.time - lastTargetUpdateTime >= targetPositionUpdateInterval)
            return true;

        // Update if current target is too close or too far from player
        if (GameController.currentPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(currentAITargetPosition, GameController.currentPlayer.transform.position);
            if (distanceToPlayer < minDistanceToPlayer || distanceToPlayer > maxDistanceToPlayer)
                return true;

            // Also update target if player has significantly changed direction
            if (Vector3.Dot(transform.forward, GameController.currentPlayer.transform.forward) < 0.7f)
                return true;
        }

        return false;
    }

    private Vector3 GetNaturalMovement()
    {
        // Create a subtle wave-like motion
        float time = Time.time;
        Vector3 naturalMotion = new Vector3(
            Mathf.Sin(time * 1.0f) * 0.3f,
            Mathf.Sin(time * 0.8f) * 0.2f,
            Mathf.Sin(time * 1.2f) * 0.3f
        );

        return naturalMotion * naturalMovementIntensity;
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

            // Check if we need to update the target position
            if (ShouldUpdateTargetPosition())
            {
                UpdateAITargetPosition();
            }

            GameObject currentPlayer = GameController.currentPlayer;
            if (currentPlayer != null)
            {
                // Calculate distance to player for basic tracking
                Vector3 directionToPlayer = currentPlayer.transform.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;

                // Calculate direction to target position
                Vector3 directionToTarget = currentAITargetPosition - transform.position;
                float distanceToTarget = directionToTarget.magnitude;

                // Default target speed
                float targetSpeed = maxForwardSpeed;

                // Check if player is sprinting using FishSchoolManager
                bool playerSprinting = FishSchoolManager.Instance != null && FishSchoolManager.Instance.IsPlayerSprinting();
                float playerCurrentSpeed = FishSchoolManager.Instance != null ? FishSchoolManager.Instance.GetPlayerSpeed() : 0f;

                // Get obstacle avoidance influence
                Vector3 obstacleInfluence = Vector3.zero;
                if (FishSchoolManager.Instance != null)
                {
                    // Use the schooling influence with forward direction for obstacle detection
                    obstacleInfluence = FishSchoolManager.Instance.GetSchoolingInfluence(transform.position, inWater, transform.forward);
                }

                // Sprint logic - improved to keep up with player
                if (playerSprinting || distanceToPlayer > 10f) // Reduced from 12f to react sooner
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
                        // Base sprint multiplier - increased lower bound
                        float sprintMultiplier = UnityEngine.Random.Range(2.2f, 2.6f);

                        // Extra boost when falling behind player
                        if (distanceToPlayer > 15f && Vector3.Dot(transform.forward, directionToPlayer.normalized) > 0.5f)
                        {
                            sprintMultiplier *= sprintCatchupMultiplier * 1.2f;  // Increased catchup multiplier
                        }

                        // Match player's speed more closely when they're sprinting
                        if (playerSprinting && playerCurrentSpeed > 0)
                        {
                            float matchFactor = Mathf.Clamp01(playerCurrentSpeed / 12f);
                            sprintMultiplier = Mathf.Lerp(sprintMultiplier, playerCurrentSpeed / maxForwardSpeed, matchFactor);
                        }

                        targetSpeed = maxForwardSpeed * sprintMultiplier;

                        // Energy cost - reduced to allow longer sprinting
                        float energyFactor = sprintMultiplier / 3.0f;  // Reduced from 2.0f
                        float energyCost = 15f * Time.fixedDeltaTime * energyFactor; // Reduced from 20f
                        playerStats.TryUseEnergy(energyCost);
                    }
                }
                else
                {
                    isAISprinting = false;
                }

                // Add natural movement to make fish motion more realistic
                Vector3 naturalMovement = GetNaturalMovement();

                // Get player direction from FishSchoolManager
                Vector3 playerDirection = FishSchoolManager.Instance != null ?
                    FishSchoolManager.Instance.GetPlayerDirection() : currentPlayer.transform.forward;

                // Blend direction to target with player direction and obstacle avoidance
                float blendFactor = playerInfluenceFactor;

                // Increase player direction influence when sprinting
                if (playerSprinting && isAISprinting) blendFactor = 0.85f;

                // Create a weighted direction that considers:
                // 1. Direction to target position
                // 2. Player's forward direction
                // 3. Obstacle avoidance influence
                Vector3 blendedDirection;

                // If obstacles are detected (magnitude > 0), prioritize avoiding them
                if (obstacleInfluence.magnitude > 0.1f)
                {
                    // Weighted blend of all influences
                    blendedDirection = Vector3.Normalize(
                        directionToTarget.normalized * 0.3f +  // Reduced target weight
                        playerDirection * 0.3f +              // Reduced player direction weight
                        obstacleInfluence.normalized * 0.4f   // Higher obstacle avoidance weight
                    );
                }
                else
                {
                    // Normal blend with target position and player direction
                    blendedDirection = Vector3.Slerp(
                        directionToTarget.normalized,
                        playerDirection,
                        blendFactor
                    );
                }

                // Combine target direction with natural movement
                Vector3 finalDirection = (blendedDirection.normalized + naturalMovement).normalized;

                // If too close to target, slow down
                if (distanceToTarget < 2f)
                {
                    targetSpeed *= 0.5f;
                }

                // IMPROVED ROTATION SMOOTHING: Using SmoothDamp for rotation
                if (finalDirection != Vector3.zero)
                {
                    // Create target rotation
                    targetRotation = Quaternion.LookRotation(finalDirection);

                    // Different rotation speeds based on situation
                    float rotateSpeed = rotationSpeed;
                    float smoothTime = rotationSmoothTime;

                    // Faster rotation when player direction differs greatly from fish direction
                    if (Vector3.Dot(transform.forward, playerDirection) < 0.7f)
                    {
                        smoothTime *= 0.5f;  // Halve smooth time for faster turning
                    }

                    // Even faster turns when falling behind during sprinting
                    if (playerSprinting && distanceToPlayer > 15f)
                    {
                        smoothTime *= 0.7f;
                    }

                    // Apply smoothed rotation
                    Vector3 currentEuler = transform.rotation.eulerAngles;
                    Vector3 targetEuler = targetRotation.eulerAngles;

                    // Handle the 0-360 wraparound for smooth rotation
                    if (targetEuler.y - currentEuler.y > 180) targetEuler.y -= 360;
                    if (targetEuler.y - currentEuler.y < -180) targetEuler.y += 360;

                    // Similar handling for pitch (x) axis
                    if (targetEuler.x - currentEuler.x > 180) targetEuler.x -= 360;
                    if (targetEuler.x - currentEuler.x < -180) targetEuler.x += 360;

                    // Limit extreme downward pitch to prevent diving behavior
                    if (targetEuler.x > 30 && targetEuler.x < 180)
                    {
                        targetEuler.x = 30;
                    }

                    // Apply smoothing to each axis separately
                    Vector3 smoothedEuler = Vector3.SmoothDamp(
                        currentEuler,
                        targetEuler,
                        ref currentRotationVelocity,
                        smoothTime
                    );

                    transform.rotation = Quaternion.Euler(smoothedEuler);
                }

                // Apply movement
                if (distanceToTarget > 1f)
                {
                    // Accelerate or decelerate to target speed
                    float accel = isAISprinting ? baseAcceleration * 4f : baseAcceleration * 1.5f; // Increased base accel

                    // Additional acceleration when falling behind during sprint
                    if (isAISprinting && distanceToPlayer > 15f)
                    {
                        accel *= 2.0f; // Significantly increased from 1.5f
                    }

                    // Even more acceleration for extreme catch-up
                    if (distanceToPlayer > 25f)
                    {
                        accel *= 1.5f;
                        targetSpeed *= 1.2f; // Allow faster than normal speeds when far behind
                    }

                    movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, accel * Time.fixedDeltaTime);
                }
                else
                {
                    // Slow down when close to target
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