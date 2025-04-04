using UnityEngine;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;
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
    [SerializeField] private float struggleCost = 20f;
    [SerializeField] private float waterExitEnergyCost = 20f;
    [SerializeField] private float aiEnergyCostMultiplier = 0.5f; // How much energy AI fish use compared to player
    [SerializeField] private float aiSprintEnergyCostPerSecond = 10f; // Energy cost per second when AI fish sprints

    private float lastSprintDamageTime;
    private float lastHurtTime;
    private float movementSpeed;
    private float targetMovementSpeed;

    private Animator fishAnimator;
    private bool isStruggling;
    private bool inWater;
    private bool isBeached;
    private PredatorAI currentPredator;
    private Rigidbody rb;
    private bool canPitchUp = true;
    private bool isInputBlocked = false;
    public bool IsStruggling => isStruggling;
    public bool InWater => inWater;

    [Header("AI Settings")]
    [SerializeField] private float struggleInterval = 0.5f; // How often to attempt struggle
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations
    [SerializeField] private float waterSurfaceYPosition = 0f; // Y position of water surface
    [SerializeField] private float independentMovementStrength = 0.5f; // How much fish move on their own
    [SerializeField] private float forwardBiasStrength = 0.7f; // Tendency to swim forward
    [SerializeField] private float naturalMovementSpeed = 0.8f; // Base speed for natural movement
    [SerializeField] private float positionUpdateInterval = 3f; // How often to update natural position

    // For rotation smoothing
    private Quaternion targetRotation;

    // For fixed positioning around player
    private Vector3 relativeOffset = Vector3.zero;
    private float uniqueAngle = 0f;

    // For natural movement
    private Vector3 naturalMovementVector;
    private float lastNaturalUpdateTime;
    private float lastStruggleTime;
    private float independentSpeedFactor;
    private bool preferFrontPosition = false;

    // For AI fish energy management
    private float lastEnergyUpdateTime;
    private float energyUpdateInterval = 0.5f;

    [Header("Level-Specific Settings")]
    [SerializeField] private float[] levelSpeedMultipliers = new float[] { 1f, 1.2f, 1.5f }; // Ocean, River, Spawning Ground
    [SerializeField] private float[] levelEnergyUseMultipliers = new float[] { 1f, 1.3f, 1.6f }; // Increased energy use in higher levels

    private void Start()
    {
        fishAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
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

        // Subscribe to player stats events for UI updates
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += OnHealthChanged;
            playerStats.OnEnergyChanged += OnEnergyChanged;
        }

        // Sync UI with initial player stats
        if (uiManager != null && playerStats.IsCurrentPlayer)
        {
            uiManager.SetHealth(playerStats.CurrentHealth);
            uiManager.SetEnergy(playerStats.CurrentEnergy);
        }

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
            preferFrontPosition = (fishID % 3 != 0); // About 2/3 of fish prefer to be in front

            // Create variation in formation - allow positions all around player, not just behind
            float horizontalRadius = UnityEngine.Random.Range(1.5f, 3.5f);
            float verticalOffset = UnityEngine.Random.Range(-1f, 1f);
            float forwardOffset = preferFrontPosition ?
                UnityEngine.Random.Range(1.5f, 10f) : // Front fish go further ahead
                UnityEngine.Random.Range(-3f, 0f);    // Back fish stay closer

            relativeOffset = new Vector3(
                Mathf.Sin(uniqueAngle) * horizontalRadius,
                verticalOffset,
                Mathf.Cos(uniqueAngle) * horizontalRadius + forwardOffset
            );

            // Initialize natural movement values
            UpdateNaturalMovement();
            independentSpeedFactor = UnityEngine.Random.Range(0.7f, 1.3f);
            lastNaturalUpdateTime = Time.time;
            lastEnergyUpdateTime = Time.time;
        }
    }

    private void UpdateNaturalMovement()
    {
        // Create random movement vector for independent motion
        naturalMovementVector = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-0.5f, 0.5f), // Less vertical movement
            UnityEngine.Random.Range(-0.5f, 1.5f)  // Bias toward forward movement
        ).normalized * independentMovementStrength;

        // Add additional forward bias for front-positioned fish
        if (preferFrontPosition)
        {
            naturalMovementVector += Vector3.forward * forwardBiasStrength * 1.2f;
            naturalMovementVector = naturalMovementVector.normalized * independentMovementStrength;
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= OnPlayerDeath;
            playerStats.OnAIFishDeath -= OnAIFishDeath;
            playerStats.OnHealthChanged -= OnHealthChanged;
            playerStats.OnEnergyChanged -= OnEnergyChanged;
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
        Debug.Log($"PlayerMovement detected AI fish death: {deadFish.name}");

        // Notify GameController about AI fish death
        if (gameController != null)
        {
            gameController.OnAIFishDied(deadFish);
        }
        else
        {
            Debug.LogError("Cannot notify GameController of AI fish death - gameController reference is null");

            // Try to find the GameController as a fallback
            gameController = FindFirstObjectByType<GameController>();
            if (gameController != null)
            {
                gameController.OnAIFishDied(deadFish);
            }
        }
    }

    // Event handlers for updating UI
    private void OnHealthChanged(float newHealth)
    {
        if (playerStats.IsCurrentPlayer && uiManager != null)
        {
            uiManager.SetHealth(newHealth);
        }
    }

    private void OnEnergyChanged(float newEnergy)
    {
        if (playerStats.IsCurrentPlayer && uiManager != null)
        {
            uiManager.SetEnergy(newEnergy);
        }
    }

    void FixedUpdate()
    {
        fishAnimator.SetBool("InWater", inWater);
        fishAnimator.SetBool("OnLand", isBeached);
        fishAnimator.SetBool("Struggling", isStruggling);
        Vector3 movement;

        // Only control if this is the current player
        if (!playerStats.IsCurrentPlayer)
        {
            if (!isStruggling)
            {
                GameObject currentPlayer = GameController.currentPlayer;
                if (currentPlayer != null)
                {
                    // Get player transform and movement data
                    Transform playerTransform = currentPlayer.transform;
                    PlayerMovement playerMovement = currentPlayer.GetComponent<PlayerMovement>();
                    if (!playerMovement.IsStruggling)
                    {
                        // Update natural movement periodically
                        if (Time.time - lastNaturalUpdateTime > positionUpdateInterval)
                        {
                            UpdateNaturalMovement();
                            lastNaturalUpdateTime = Time.time;
                        }

                        // Handle AI fish energy updates
                        if (Time.time - lastEnergyUpdateTime > energyUpdateInterval)
                        {
                            UpdateAIFishEnergy();
                            lastEnergyUpdateTime = Time.time;
                        }

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

                        // Determine if we should face player's direction or move toward position
                        float returnThreshold = 7f; // Distance threshold where fish prioritizes returning to formation
                        bool shouldFacePlayer = distanceToTarget < returnThreshold;

                        if (shouldFacePlayer)
                        {
                            // Use FishSchoolManager to get player's facing direction with variation
                            FishSchoolManager schoolManager = FishSchoolManager.Instance;
                            if (schoolManager != null)
                            {
                                // Get player's rotation with small variation based on uniqueAngle
                                targetRotation = schoolManager.GetPlayerFacingRotation(uniqueAngle);
                            }
                            else
                            {
                                // Fallback to direct player rotation with manual variation
                                targetRotation = playerTransform.rotation;

                                // Add slight random variation
                                float randomYaw = Mathf.Sin(Time.time * 0.8f + uniqueAngle * 10) * 10f;
                                float randomPitch = Mathf.Sin(Time.time * 0.6f + uniqueAngle * 5) * 5f;
                                targetRotation *= Quaternion.Euler(randomPitch, randomYaw, 0);
                            }
                        }
                        else
                        {
                            // When too far away, prioritize returning to formation
                            if (directionToTarget.magnitude > 0.01f)
                            {
                                targetRotation = Quaternion.LookRotation(directionToTarget);
                            }
                        }

                        // Apply smoothed rotation - faster when returning to formation
                        float rotationFactor = shouldFacePlayer ? (10f / rotationSmoothTime) : (15f / rotationSmoothTime);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            Time.fixedDeltaTime * rotationFactor
                        );

                        // Set base movement speed - always moving somewhat forward on their own
                        float baseSpeed = naturalMovementSpeed * independentSpeedFactor;

                        // Adjust speed based on distance to target
                        float targetSpeed = baseSpeed;
                        bool isSprinting = false;

                        if (distanceToTarget > returnThreshold)
                        {
                            // Catch up if too far away
                            targetSpeed = playerSpeed * 1.5f;
                            isSprinting = true;
                        }
                        else if (distanceToTarget > 3f)
                        {
                            // Match speed with slight increase if not in ideal position
                            targetSpeed = Mathf.Max(baseSpeed, playerSpeed * 0.9f);
                        }
                        else
                        {
                            // In position, match player speed exactly
                            targetSpeed = playerSpeed;

                            // Check if player is sprinting
                            FishSchoolManager schoolManager = FishSchoolManager.Instance;
                            if (schoolManager != null && schoolManager.IsPlayerSprinting())
                            {
                                targetSpeed = playerSpeed;  // Match player sprint speed
                                isSprinting = true;
                            }
                        }

                        // Apply energy cost for AI fish sprinting
                        if (isSprinting && inWater)
                        {
                            float energyCost = aiSprintEnergyCostPerSecond * Time.fixedDeltaTime;
                            float speedFactor = targetSpeed / maxForwardSpeed;

                            if (playerStats.CurrentEnergy < energyCost)
                            {
                                // Apply energy and damage
                                ApplyEnergyAndDamageForAI(energyCost, speedFactor);

                                // If not enough energy, reduce speed
                                targetSpeed = Mathf.Min(targetSpeed, playerSpeed * 0.7f);
                            }
                            else
                            {
                                // Have enough energy, just use it
                                playerStats.ModifyEnergy(-energyCost);
                            }
                        }
                        else if (movementSpeed > naturalMovementSpeed * 1.2f)
                        {
                            // Apply energy cost for fast movement even when not sprinting
                            float speedFactor = movementSpeed / maxForwardSpeed;
                            float energyCost = (aiSprintEnergyCostPerSecond * 0.5f) * speedFactor * Time.fixedDeltaTime;
                            playerStats.ModifyEnergy(-energyCost);
                        }

                        // Smooth acceleration to target speed
                        movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, baseAcceleration * 1.5f * Time.fixedDeltaTime);

                        // Apply movement in the direction the fish is facing
                        movement = transform.forward * movementSpeed;

                        if (isBeached)
                        {
                            rb.AddForce(10 * movement + 20 * Vector3.up);
                        }
                    }
                    else
                        movement = Vector3.zero;
                }
                else
                    movement = Vector3.zero;
            }
            else if (isStruggling && currentPredator != null) //if ai struggling
            {
                if (Time.time - lastStruggleTime >= struggleInterval)
                {
                    if (playerStats.TryUseEnergy(struggleCost))//check if enough energy to struggle, use stamina
                    {
                        int escape = Random.Range(1, 6);
                        if (escape == 1) // 1 in 5 chance to escape per attempt
                        {
                            currentPredator.EndStruggle(false);
                            isStruggling = false;
                        }
                    }
                    lastStruggleTime = Time.time;
                }
                movement = Vector3.zero; //fix fish position, rotation to predator mouth
                transform.rotation = Quaternion.Euler(currentPredator.transform.eulerAngles + currentPredator.FeedingRotationOffset);
                transform.position = currentPredator.transform.position + currentPredator.transform.TransformDirection(currentPredator.FeedingOffset);
                if (Time.time - lastHurtTime >= currentPredator.AttackCooldown)
                {
                    AudioManager.i.PlaySfx(SfxId.Crunch);
                    //if ai fish dies from this bite, release predator
                    if (playerStats.CurrentHealth - currentPredator.AttackDmg <= 0)
                    {
                        currentPredator.EndStruggle(true);
                    }
                    playerStats.ModifyHealth(-currentPredator.AttackDmg);
                    lastHurtTime = Time.time;
                }
            }
            else //if struggling but no predator, release
            {
                movement = Vector3.zero;
                isStruggling = false;
            }
        }
        else // If Player fish
        {
            if (!isStruggling && !isInputBlocked) // Check for input blocking
            {
                float moveInput = 0f;
                // Handle rotation and tilt input

                float yawInput = Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A) ? 1f : (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) ? -1f : 0f);

                // Only process turning when in water or when beached and space is pressed
                bool canTurn = inWater || (isBeached && Input.GetKey(KeyCode.Space));

                if (canTurn && Mathf.Abs(movementSpeed) < 5f && yawInput != 0)
                {
                    if (yawInput == 1f)
                    {
                        fishAnimator.SetBool("TurnRight", true);
                        fishAnimator.SetBool("TurnLeft", false);
                        moveInput = 0.3f;
                    }
                    else if (yawInput == -1f)
                    {
                        fishAnimator.SetBool("TurnLeft", true);
                        fishAnimator.SetBool("TurnRight", false);
                        moveInput = 0.3f;
                    }
                }
                else
                {
                    fishAnimator.SetBool("TurnLeft", false);
                    fishAnimator.SetBool("TurnRight", false);
                }

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

                // Apply rotations - use actual rotation speed for both in water and on land
                float effectiveRotationSpeed = inWater ? rotationSpeed : 150f;

                // Only apply rotation if we can turn (in water or beached with space pressed)
                if (canTurn)
                {
                    transform.Rotate(Vector3.up, yawInput * effectiveRotationSpeed * Time.deltaTime);
                }

                // Calculate and apply tilt
                float yaw = yawInput * yawAmount;
                Quaternion targetRotation = Quaternion.Euler(pitch, transform.eulerAngles.y, yaw);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);

                // Handle speed changes based on shift/ctrl
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
                if (isBeached && moveInput > 0)
                {
                    rb.AddForce(10 * movement + 20 * Vector3.up);
                }
            }
            else if (isStruggling && currentPredator != null) // if struggling
            {
                //check struggle input
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (playerStats.TryUseEnergy(struggleCost))//check if enough energy to struggle, use stamina
                    {
                        uiManager.SetEnergy(playerStats.CurrentEnergy);
                        int escape = Random.Range(1, 6);
                        if (escape == 1) // 1 in 5 chance to escape per attempt
                        {
                            currentPredator.EndStruggle(false);
                            isStruggling = false;

                            // Hide struggle instructions when player escapes
                            if (gameController != null)
                            {
                                gameController.HideInstructionPanel();
                            }
                        }
                    }
                }
                movement = Vector3.zero; //fix fish position, rotation to predator mouth
                transform.rotation = Quaternion.Euler(currentPredator.transform.eulerAngles + currentPredator.FeedingRotationOffset);
                transform.position = currentPredator.transform.position + currentPredator.transform.TransformDirection(currentPredator.FeedingOffset);
                if (Time.time - lastHurtTime >= currentPredator.AttackCooldown)
                {
                    AudioManager.i.PlaySfx(SfxId.Crunch);
                    //if player dies from this bite, release predator
                    if (playerStats.CurrentHealth - currentPredator.AttackDmg <= 0)
                    {
                        currentPredator.EndStruggle(true);

                        // Hide struggle instructions when player dies
                        if (gameController != null)
                        {
                            gameController.HideInstructionPanel();
                        }
                    }
                    gameController.OnPlayerDamaged(currentPredator.AttackDmg);
                    lastHurtTime = Time.time;
                }
            }
            else //if struggling but no predator, release
            {
                movement = Vector3.zero;
                isStruggling = false;

                // Hide struggle instructions when player is released
                if (gameController != null)
                {
                    gameController.HideInstructionPanel();
                }
            }
        }

        if (inWater && !isStruggling)
        {
            rb.AddForce(movement);
            isBeached = false;
        }
        else if (!isStruggling)
        {
            if (Mathf.Abs(rb.linearVelocity.y) < 0.001f)
            {
                if (!isBeached)
                    AudioManager.i.PlaySfx(SfxId.FishFlopping);
                isBeached = true; //grounded
            }
            else
                isBeached = false;
            
        }

        if (!isStruggling)
            eForce.force = eForceDir;

        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", movementSpeed);
        }
    }

    // New method to handle AI fish energy
    private void UpdateAIFishEnergy()
    {
        if (playerStats == null || playerStats.IsCurrentPlayer) return;

        // AI fish have more efficient energy management
        float minEnergyThreshold = 20f;

        // If energy is below threshold, ensure they recover faster than players
        if (playerStats.CurrentEnergy < minEnergyThreshold)
        {
            float recoveryAmount = 2.0f * energyUpdateInterval;
            playerStats.ModifyEnergy(recoveryAmount);
        }
    }

    // Helper method to apply energy cost and damage AI fish if needed
    private void ApplyEnergyAndDamageForAI(float energyCost, float speedFactor)
    {
        if (playerStats == null) return;

        if (playerStats.CurrentEnergy >= energyCost)
        {
            playerStats.ModifyEnergy(-energyCost);
        }
        else
        {
            // Not enough energy - use all remaining energy
            float remainingEnergy = playerStats.CurrentEnergy;
            playerStats.SetEnergy(0);

            // Apply health damage proportional to energy deficit, but never below 50% health
            float healthDamage = (energyCost - remainingEnergy) * 0.3f * speedFactor;
            float minHealth = playerStats.MaxHealth * 0.5f;

            if (playerStats.CurrentHealth > minHealth)
            {
                // Only apply damage if above 50% health threshold
                float newHealth = Mathf.Max(minHealth, playerStats.CurrentHealth - healthDamage);
                playerStats.SetHealth(newHealth);
            }
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        isInputBlocked = blocked;

        // If blocking input, also reset movement speed
        if (blocked)
        {
            movementSpeed = 0f;
            targetMovementSpeed = 0f;

            // Also reset any rigidbody velocity
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ResetMovementState()
    {
        // Reset all movement and physics-related values
        movementSpeed = 0f;
        targetMovementSpeed = 0f;

        // Reset animation parameters if necessary
        if (fishAnimator != null)
        {
            fishAnimator.SetFloat("Speed", 0f);
            fishAnimator.SetBool("TurnRight", false);
            fishAnimator.SetBool("TurnLeft", false);
        }

        // Reset any pending forces or states
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    { //in water
        if (!inWater)
            AudioManager.i.PlaySfx(SfxId.Splash);
        if (other.gameObject.CompareTag("Water"))
        {
            inWater = true;
            rb.linearDamping = 1.5f;
            rotationSpeed = 100f;
            maxForwardSpeed = 5f;
            eForceDir = new Vector3(0, 0, 0);

            // Update player stats with water state
            if (playerStats != null)
            {
                playerStats.SetInWater(true);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isStruggling)
        {
            if (other.gameObject.CompareTag("Predator"))
            {
                currentPredator = other.gameObject.GetComponent<PredatorAI>();
                if (currentPredator)
                {
                    if (currentPredator.CanAttack)
                    {
                        isStruggling = true;
                        AudioManager.i.PlaySfx(SfxId.Crunch);
                        currentPredator.StartStruggle();

                        // Show struggle instructions when player is grabbed by predator
                        if (gameController != null && playerStats.IsCurrentPlayer)
                        {
                            gameController.ShowInstructionPanel("In a struggle", "Spam the spacebar to escape");
                        }
                    }
                }
            } // We'll handle rotation differently when beached
        }
    }

    private void OnTriggerExit(Collider other)
    { //out of water
        if (other.gameObject.CompareTag("Water"))
        {
            if (inWater)
                AudioManager.i.PlaySfx(SfxId.Splash);
            inWater = false;
            rb.linearDamping = 0.1f;
            rotationSpeed = 0f;
            maxForwardSpeed = 0.5f;
            eForceDir = new Vector3(0, -3, 0);
            canPitchUp = false;

            // Update player stats with water state
            if (playerStats != null)
            {
                playerStats.SetInWater(false);
            }

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
                float aiEnergyCost = waterExitEnergyCost * aiEnergyCostMultiplier;

                // Apply energy cost and potential damage
                float speedFactor = 1.0f;
                ApplyEnergyAndDamageForAI(aiEnergyCost, speedFactor);
            }
        }
    }

    // Add method to force set water state
    public void ForceSetWaterState(bool inWaterState)
    {
        inWater = inWaterState;
        if (playerStats != null)
        {
            playerStats.SetInWater(inWaterState);
        }
    }
}