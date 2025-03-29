using UnityEngine;
using System;
using Unity.Mathematics;

public class SealAI : PredatorAI
{
    enum ActivityState { Surfacing, Floating, Hunting, Feeding }

    [Header("Breath Settings")]
    [SerializeField] private float maxBreathTime = 120f;
    [SerializeField] private float maxSurfaceTime = 10f;
    private float surfaceTime;
    private float currentBreath;
    [SerializeField] private float breathCostPerSecond = 1f; // breath cost per second when seal underwater
    [SerializeField] private float breathGainPerSecond = 50f; // breath gain per second when seal out of water

    [Header("Movement Settings")]
    [SerializeField] private float maxForwardSpeed = 12f;
    [SerializeField] private float baseAcceleration = 7f;
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations
    private ConstantForce eForce; // external force (river current, gravity, water buoyancy)
    private Vector3 eForceDir; // net direction of external force
    private float movementSpeed;
    private float maxSpeed;
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
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        inWater = true;
        actState = ActivityState.Surfacing;
        currentBreath = maxBreathTime;
        surfaceTime = 0;
        eForce = GetComponent<ConstantForce>();
        rb.useGravity = false;
        canAttack = false;
        maxSpeed = maxForwardSpeed;
    }

    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        float distanceToPlayer;
        if (player == null) // if no active player, surface
        {
            actState = ActivityState.Surfacing;
            distanceToPlayer = Mathf.Infinity;
        }
        else
        {
            playerMove = player.GetComponent<PlayerMovement>();
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }

        float breathUseMultiplier = 1f;
        if (currentBreath <= 0) // return to surface if breath runs out
        {
            actState = ActivityState.Surfacing;
        }
        Vector3 targetPosition;
        canAttack = actState == ActivityState.Hunting;

        switch (actState) //determine seal behaviour based on activity state
        {
            case ActivityState.Surfacing: //if surfacing, head to surface
                movement = Vector3.up;
                if (canBreathe && surfaceTime == 0) // once at surface, set for surface time
                    surfaceTime = maxSurfaceTime;
                else
                    surfaceTime = Mathf.Clamp(surfaceTime - Time.fixedDeltaTime, 0, Mathf.Infinity);
                if (currentBreath == maxBreathTime && surfaceTime <= 0) // proceed to float (look for prey) once surface time over
                {
                    actState = ActivityState.Floating;
                }
                break;
            case ActivityState.Floating:
                movement = Vector3.zero;
                breathUseMultiplier = 0.5f; // reduce breath use rate by half if floating
                if (distanceToPlayer <= detectionRadius && CanSeePlayer()) //if can detect player, pursue
                {
                    if (!playerMove.IsStruggling && playerMove.InWater)
                        actState = ActivityState.Hunting;
                }
                break;
            case ActivityState.Hunting:
                if (playerMove.IsStruggling)
                {
                    actState = ActivityState.Floating;
                    break;
                }
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
                movementSpeed = Mathf.MoveTowards(movementSpeed, maxSpeed, baseAcceleration * 1.5f * Time.fixedDeltaTime);
                // Apply movement in the direction the seal is facing
                movement = transform.forward * movementSpeed;

                if (isBeached)
                    rb.AddForce(10 * movement + 20 * Vector3.up);
                break;
            case ActivityState.Feeding:
                movement = Vector3.up;
                targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                // Apply smoothed rotation
                float rFactor = 10f / rotationSmoothTime;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * rFactor
                );
                break;
        }
        // Recover breath if breached or surfacing
        if (canBreathe)
        {
            float breathGain = breathGainPerSecond * Time.fixedDeltaTime; //restore half of breath per second
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

        if (anim != null)
        {
            HandleSealAnims(actState);
            anim.SetFloat("Speed", movementSpeed);
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
            maxSpeed = maxForwardSpeed;
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
            if (inWater)
                AudioManager.i.PlaySfx(SfxId.Splash);
            inWater = false;
            canBreathe = true;
            rb.linearDamping = 0.1f;
            maxSpeed = 0.5f;
            eForceDir = new Vector3(0, -3, 0);
        }
    }

    public override void StartStruggle()
    {
        anim.SetBool("Feeding", true);
        actState = ActivityState.Feeding;
    }

    public override void EndStruggle(bool success)
    {
        anim.SetBool("Feeding", false);
        if (success)
            surfaceTime = maxSurfaceTime * 2; //double surface time if successful catch
        else
            surfaceTime = maxSurfaceTime;
        actState = ActivityState.Surfacing;
    }

    private void HandleSealAnims(ActivityState action)
    {
        anim.SetBool("InWater", inWater);
        switch (action) //handle activity based anims
        {
            case ActivityState.Surfacing:
                anim.SetBool("Feeding", false);
                anim.SetBool("Surfacing", true);
                anim.SetBool("Floating", false);
                break;
            case ActivityState.Floating:
                anim.SetBool("Feeding", false);
                anim.SetBool("Surfacing", false);
                anim.SetBool("Floating", true);
                break;
            case ActivityState.Feeding:
                anim.SetBool("Feeding", true);
                anim.SetBool("Surfacing", false);
                anim.SetBool("Floating", false);
                break;
            default:
                anim.SetBool("Feeding", false);
                anim.SetBool("Surfacing", false);
                anim.SetBool("Floating", false);
                break;
        }
    }
}