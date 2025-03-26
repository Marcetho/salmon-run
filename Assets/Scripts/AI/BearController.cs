using UnityEngine;
using System;
using Unity.Mathematics;

public class BearController : PredatorAI
{
    enum BearState { Fishing, Carrying, Feeding }

    [Header("Bear Settings")]
    [SerializeField] private Vector3 fishingSpot;
    [SerializeField] private Vector3 feedingSpot;

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
                break;
            case BearState.Carrying:
                break;
            case BearState.Feeding:
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

    private void HandleBearAnims(BearState action)
    {
        switch (action) //handle activity based anims
        {
            case BearState.Fishing:
                anim.SetBool("Feeding", false);
                anim.SetBool("Fishing", true);
                anim.SetBool("Carrying", false);
                break;
            case BearState.Carrying:
                anim.SetBool("Feeding", false);
                anim.SetBool("Fishing", false);
                anim.SetBool("Carrying", true);
                break;
            case BearState.Feeding:
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
