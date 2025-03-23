using UnityEngine;
using System;

public class SealAI : MonoBehaviour
{
    private GameController gameController;

    [Header("Predator Stats")]
    public float detectionRadius = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float maxStamina = 100f;
    private float currentStamina;
    [SerializeField] private float sprintEnergyCostPerSecond = 10f; // Energy cost per second when AI seal sprints

    [Header("Movement Settings")]
    public float maxForwardSpeed = 5f;
    public float baseAcceleration = 4f;
    public float baseDeceleration = 16f;
    public float rotationSpeed = 100f;
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations
    private ConstantForce eForce; // external force (river current, gravity, water buoyancy)
    private Vector3 eForceDir; // net direction of external force

    [Header("Energy Settings")]
    
    private GameObject player;
    private float movementSpeed;
    private float baseYPosition;
    private Animator sealAnimator;
    private bool inWater;
    private bool isBeached;
    private Rigidbody rb;
    private bool canPitchUp = true;

    // For rotation smoothing
    private Quaternion targetRotation;

    // For AI seal energy management
    private float lastEnergyUpdateTime;
    private float energyUpdateInterval = 0.5f;

    private void Start()
    {
        gameController = FindFirstObjectByType<GameController>();
        sealAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;
        inWater = true;
        currentStamina = maxStamina;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        sealAnimator.SetBool("InWater", inWater);
        sealAnimator.SetBool("OnLand", isBeached);
        Vector3 movement;

        GameObject currentPlayer = GameController.currentPlayer;
        if (currentPlayer != null)
        {
            // Handle AI seal energy updates
            if (Time.time - lastEnergyUpdateTime > energyUpdateInterval)
            {
                lastEnergyUpdateTime = Time.time;
            }
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            // Get player transform
            Vector3 targetPosition = currentPlayer.transform.position;

            // Calculate direction to target position
            Vector3 directionToTarget = targetPosition - transform.position;

            // look towards target
            targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // Apply smoothed rotation
            float rotationFactor = 10f / rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * rotationFactor
                );

            // Adjust speed based on distance to target
            float targetSpeed = maxForwardSpeed;
            bool isSprinting = false;

            // Apply energy cost for AI seal sprinting
            if (isSprinting && inWater)
            {
                float energyCost = sprintEnergyCostPerSecond * Time.fixedDeltaTime;

                if (currentStamina < energyCost)
                {
                    currentStamina = 0;
                    // If not enough energy, surface
                    // SURFACE HERE
                }
                else
                    currentStamina -= energyCost;
            }

            // Smooth acceleration to target speed
            movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, baseAcceleration * 1.5f * Time.fixedDeltaTime);

            // Apply movement in the direction the seal is facing
            movement = transform.forward * movementSpeed;

            if (isBeached)
                rb.AddForce(10 * movement + 20 * Vector3.up);
        }
        else
            movement = Vector3.zero;

        if (inWater)
        {
            rb.AddForce(movement);
            isBeached = false;
        }
        else
            isBeached = Mathf.Abs(rb.linearVelocity.y) < 0.001f; //grounded

        eForce.force = eForceDir;

        if (sealAnimator != null)
        {
            sealAnimator.SetFloat("Speed", movementSpeed);
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        RaycastHit hit;

        // Define a layer mask that ignores the water layer
        int layerMask = ~(1 << LayerMask.NameToLayer("TransparentFX")); // Exclude Water layer

        if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRadius, layerMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
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
        }
    }
}