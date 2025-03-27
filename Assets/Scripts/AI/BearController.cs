using UnityEngine;
using System;
using Unity.Mathematics;

public class BearController : PredatorAI
{
    enum BearState { Fishing, Carrying, Feeding }

    [Header("Bear Settings")]
    [SerializeField] private Transform fishingSpot;
    [SerializeField] private Transform feedingSpot;
    [SerializeField] private int feedingRadius;
    [SerializeField] private float carryAtkCooldown = 10f;

    [SerializeField] private float feedAtkCooldown = 1f;
    [SerializeField] private Vector3 carryingOffset;
    [SerializeField] private Vector3 eatingOffset;

    private UnityEngine.AI.NavMeshAgent agent;
    private BearState actState;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        actState = BearState.Fishing;
        rb.useGravity = false;
        canAttack = false;
        if (!fishingSpot || !feedingSpot)
            Debug.Log("Feeding and fishing spot for bear must be assigned!");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        canAttack = actState == BearState.Fishing;
        float distanceToPlayer;
        if (player == null) // if no active player, stand around "fishing"
        {
            actState = BearState.Fishing;
            distanceToPlayer = Mathf.Infinity;
        }
        else
        {
            playerMove = player.GetComponent<PlayerMovement>();
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }
        switch (actState)
        {
            case BearState.Fishing:
                float distanceToFishingSpot = Vector3.Distance(transform.position, fishingSpot.position);
                if (distanceToFishingSpot > feedingRadius && playerMove.InWater) //if not in fishing spot, navigate to pos
                    agent.SetDestination(fishingSpot.position); // if player is out of water, prioritize pursuing
                else // if within fishing spot, look for player
                {
                    if (distanceToPlayer <= detectionRadius && CanSeePlayer())
                        if (!playerMove.IsStruggling)
                            agent.SetDestination(player.transform.position);
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
        actState = BearState.Fishing;
        feedingOffset = carryingOffset;
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
