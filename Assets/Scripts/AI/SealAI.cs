using UnityEngine;
using System;
using System.Net.NetworkInformation;

public class SealAI : MonoBehaviour
{
    enum ActivityState {Surfacing, Floating, Hunting, Feeding}
    private GameController gameController;

    [Header("Predator Stats")]
    public Collider bodyCollider;
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float maxBreathTime = 120f;
    public float maxSurfaceTime = 10f;
    private float surfaceTime;
    private float currentBreath;
    [SerializeField] private float breathCostPerSecond = 1f; // breath cost per second when seal underwater
    [SerializeField] private float breathGainPerSecond = 50f; // breath gain per second when seal out of water

    [Header("Movement Settings")]
    public float maxForwardSpeed = 10f;
    public float baseAcceleration = 7f;
    public float baseDeceleration = 16f;
    public float rotationSpeed = 100f;
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations
    private ConstantForce eForce; // external force (river current, gravity, water buoyancy)
    private Vector3 eForceDir; // net direction of external force
    private GameObject player;
    private float movementSpeed;
    private Animator sealAnimator;
    private bool inWater;
    private bool canBreathe;
    private bool isBeached;
    private ActivityState actState;
    private Rigidbody rb;

    // For rotation smoothing
    private Quaternion targetRotation;
    private Vector3 movement = Vector3.zero;

    private void Start()
    {
        gameController = FindFirstObjectByType<GameController>();
        sealAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        inWater = true;
        actState = ActivityState.Surfacing;
        currentBreath = maxBreathTime;
        surfaceTime = 0;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        float distanceToPlayer;
        if (player == null) // if no active player, float
        {
            actState = ActivityState.Floating;
            distanceToPlayer = Mathf.Infinity;
        }
        else
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        float breathUseMultiplier = 1f;
        if (currentBreath <= 0) // return to surface if breath runs out
        {
            actState = ActivityState.Surfacing;
        }
        Vector3 targetPosition;

        switch (actState) //determine seal behaviour based on activity state
        {
            case ActivityState.Surfacing: //if surfacing, head to surface
                movement = Vector3.up;
                if (canBreathe && surfaceTime == 0) // once at surface, set for surface time
                    surfaceTime = maxSurfaceTime;
                else
                    surfaceTime = Mathf.Clamp(surfaceTime - Time.fixedDeltaTime, 0, maxSurfaceTime);
                
                if (currentBreath == maxBreathTime && surfaceTime <= 0) // proceed to float (look for prey) once surface time over
                {
                    actState = ActivityState.Floating;
                }
                break;
            case ActivityState.Floating:
                movement = Vector3.zero;
                breathUseMultiplier = 0.5f; // reduce breath use rate by half if floating
                if (distanceToPlayer <= detectionRadius && CanSeePlayer()) //if can detect player, pursue
                    actState = ActivityState.Hunting;
                break;
            case ActivityState.Hunting:
                targetPosition = player.transform.position;
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

                // Smooth acceleration to target speed
                movementSpeed = Mathf.MoveTowards(movementSpeed, maxForwardSpeed, baseAcceleration * 1.5f * Time.fixedDeltaTime);
                // Apply movement in the direction the seal is facing
                movement = transform.forward * movementSpeed;

                if (isBeached)
                    rb.AddForce(10 * movement + 20 * Vector3.up);
                break;
            case ActivityState.Feeding:
                movement = Vector3.up;
                break;
        }
        // Recover breath if breached or surfacing
        if (canBreathe)
        {
            float breathGain = breathGainPerSecond* Time.fixedDeltaTime; //restore half of breath per second
            currentBreath = Mathf.Clamp(currentBreath + breathGain, 0, maxBreathTime); 
        }
        else //otherwise exhaust breath
        {
            float breathCost = breathCostPerSecond * breathUseMultiplier * Time.fixedDeltaTime;
            currentBreath = Mathf.Clamp(currentBreath - breathCost, 0, maxBreathTime);
        }
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
            HandleSealAnims(actState);
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
            maxForwardSpeed = 10f;
            eForceDir = new Vector3(0, 0, 0);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Water"))
        {
            canBreathe = !(other.bounds.Contains(bodyCollider.bounds.max) && other.bounds.Contains(bodyCollider.bounds.min));
            // if partially submerged, can breathe
        }
    }

    private void OnTriggerExit(Collider other)
    { //out of water
        if (other.gameObject.CompareTag("Water"))
        {
            inWater = false;
            canBreathe = true;
            rb.linearDamping = 0.1f;
            rotationSpeed = 0f;
            maxForwardSpeed = 0.5f;
            eForceDir = new Vector3(0, -3, 0);
        }
    }

    private void HandleSealAnims(ActivityState action)
    {
        sealAnimator.SetBool("InWater", inWater);
        switch (action) //handle activity based anims
        {
            case ActivityState.Surfacing:
                sealAnimator.SetBool("Feeding", false);
                sealAnimator.SetBool("Surfacing", true);
                sealAnimator.SetBool("Floating", false);
                break;
            case ActivityState.Floating:
                sealAnimator.SetBool("Feeding", false);
                sealAnimator.SetBool("Surfacing", false);
                sealAnimator.SetBool("Floating", true);
                break;
            case ActivityState.Feeding:
                sealAnimator.SetBool("Feeding", true);
                sealAnimator.SetBool("Surfacing", false);
                sealAnimator.SetBool("Floating", false);
                break;
            default:
                sealAnimator.SetBool("Feeding", false);
                sealAnimator.SetBool("Surfacing", false);
                sealAnimator.SetBool("Floating", false);
                break;
        }
    }
}