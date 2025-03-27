using UnityEngine;
using System;
using Unity.Mathematics;

public class BearController : PredatorAI
{
    enum BearState { Resting, Fishing, Carrying, Feeding }

    [Header("Bear Settings")]
    [SerializeField] private float maxBreathTime = 2f; // how long bear will allow itself to be submerged for
    [SerializeField] private Transform fishingSpot; // ENSURE bear can breathe at fishing spot
    [SerializeField] private Transform feedingSpot;
    [SerializeField] private int feedingRadius;
    [SerializeField] private float carryAtkCooldown = 10f;

    [SerializeField] private float feedAtkCooldown = 1f;
    [SerializeField] private Vector3 carryingOffset;
    [SerializeField] private Vector3 eatingOffset;
    [Header("Movement Settings")]
    [SerializeField] private float rotationSmoothTime = 0.3f; // Time to smooth rotations

    private Quaternion targetRotation;
    private UnityEngine.AI.NavMeshAgent agent;
    private BearState actState;
    private Rigidbody rb;
    private float currentBreath;
    private bool canBreathe;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        actState = BearState.Fishing;
        rb.useGravity = false;
        canAttack = false;
        canBreathe = true;
        currentBreath = maxBreathTime;
        if (!fishingSpot || !feedingSpot)
            Debug.Log("Feeding and fishing spot for bear must be assigned!");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        canAttack = actState == BearState.Fishing;
        float distanceToPlayer;
        if (player == null) // if no active player, rest
        {
            actState = BearState.Resting;
            distanceToPlayer = Mathf.Infinity;
        }
        else
        {
            playerMove = player.GetComponent<PlayerMovement>();
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }
        if (!canBreathe)
            currentBreath = Mathf.Clamp(currentBreath - Time.fixedDeltaTime, 0, maxBreathTime);
        else    
            currentBreath = Mathf.Clamp(currentBreath + Time.fixedDeltaTime, 0, maxBreathTime);
        switch (actState)
        {
            case BearState.Fishing:
                if (currentBreath <= 0) // if breath runs out, return to fishing spot and rest
                {
                    agent.SetDestination(fishingSpot.position);
                    actState = BearState.Resting;   
                    break;     
                } 
                float distanceToFishingSpot = Vector3.Distance(transform.position, fishingSpot.position);
                // if breath runs out, or if away from fishing spot and player is in water, return to fishing spot
                if (distanceToFishingSpot > feedingRadius && playerMove.InWater)
                    agent.SetDestination(fishingSpot.position);
                else // if within fishing spot, look for player
                {
                    if (!playerMove.IsStruggling && distanceToPlayer <= detectionRadius && CanSeePlayer())
                    {
                        Vector3 targetPosition = player.transform.position;
                        agent.SetDestination(player.transform.position);
                        Vector3 directionToTarget = targetPosition - transform.position;
                        // look towards target
                        targetRotation = Quaternion.LookRotation(directionToTarget);
                        targetRotation = Quaternion.Euler(transform.eulerAngles.x, targetRotation.eulerAngles.y, transform.eulerAngles.z);
                        // Apply smoothed rotation
                        float rotationFactor = 10f / rotationSmoothTime;
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            Time.fixedDeltaTime * rotationFactor
                        );
                    }
                }
                break;
            case BearState.Carrying:
                float distanceToFeedingSpot = Vector3.Distance(transform.position, feedingSpot.position);
                if (distanceToFeedingSpot > feedingRadius) //if not in feeding spot, navigate to pos
                    agent.SetDestination(feedingSpot.position);
                else // if reached feeding spot, feed
                {
                    actState = BearState.Feeding;
                    attackCooldown = feedAtkCooldown;
                    feedingOffset = eatingOffset;
                }
                break;
            case BearState.Feeding:
                break;
            case BearState.Resting:
                if (currentBreath >= maxBreathTime)
                    actState = BearState.Fishing;
                break;
        }
        if (anim != null)
        {
            HandleBearAnims(actState);
            anim.SetFloat("Speed", agent.velocity.magnitude/agent.speed);
        }
    }
    public override void StartStruggle()
    {
        actState = BearState.Carrying;
        attackCooldown = carryAtkCooldown;
        feedingOffset = carryingOffset;
    }

    public override void EndStruggle(bool success)
    {
        currentBreath = 0;
        actState = BearState.Resting;
        feedingOffset = carryingOffset;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Water"))
        {
            canBreathe = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Water"))
        {
            canBreathe = true;
        }
    }

    private void HandleBearAnims(BearState action)
    {
        switch (action) //handle activity based anims
        {
            case BearState.Fishing:
                anim.SetBool("Feeding", false);
                anim.SetBool("Carrying", false);
                break;
            case BearState.Carrying:
                anim.SetBool("Feeding", false);
                anim.SetBool("Carrying", true);
                break;
            case BearState.Feeding:
                anim.SetBool("Feeding", true);
                anim.SetBool("Carrying", false);
                break;
            default:
                anim.SetBool("Feeding", false);
                anim.SetBool("Carrying", false);
                break;
        }
    }
}
