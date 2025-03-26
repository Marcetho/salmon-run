using UnityEngine;
using System;
using Unity.Mathematics;

public class BearAI : PredatorAI
{
    enum ActivityState { Fishing, Carrying, Feeding }
    private UnityEngine.AI.NavMeshAgent agent;
    private ActivityState actState;

    [Header("Bear Settings")]
    [SerializeField] private Vector3 fishingSpot;
    [SerializeField] private Vector3 feedingSpot;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        inWater = true;
        actState = ActivityState.Fishing;
        rb.useGravity = false;
        canAttack = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        player = GameController.currentPlayer;
        canAttack = actState == ActivityState.Fishing;
        float distanceToPlayer;
        if (player == null) // if no active player, stand around "fishing"
        {
            actState = ActivityState.Fishing;
            distanceToPlayer = Mathf.Infinity;
        }
        else
        {
            playerMove = player.GetComponent<PlayerMovement>();
            distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        }
        switch (actState)
        {
            case ActivityState.Fishing:
                break;
            case ActivityState.Carrying:
                break;
            case ActivityState.Feeding:
                break;
        }

        if (distanceToPlayer <= detectionRadius)
        {
            if (CanSeePlayer())
            {
                agent.SetDestination(player.transform.position);

            }
        }
    }

    private void HandleBearAnims(ActivityState action)
    {
        anim.SetBool("InWater", inWater);
        switch (action) //handle activity based anims
        {
            case ActivityState.Fishing:
                anim.SetBool("Feeding", false);
                anim.SetBool("Fishing", true);
                anim.SetBool("Carrying", false);
                break;
            case ActivityState.Carrying:
                anim.SetBool("Feeding", false);
                anim.SetBool("Fishing", false);
                anim.SetBool("Carrying", true);
                break;
            case ActivityState.Feeding:
                anim.SetBool("Feeding", true);
                anim.SetBool("Fishing", false);
                anim.SetBool("Carrying", false);
                break;
            default:
                anim.SetBool("Feeding", false);
                anim.SetBool("Fishing", false);
                anim.SetBool("Carrying", false);
                break;
        }
    }
}
