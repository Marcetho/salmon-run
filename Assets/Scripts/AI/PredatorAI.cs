using UnityEngine;
using System;

public class PredatorAI : MonoBehaviour
{
    [Header("References")]
    public GameController gameController;
    public GameObject player;
    public Animator anim;

    [Header("Stats")]
    public Collider bodyCollider;
    public float detectionRadius = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    [Header("Visual")]
    public Transform jawBone;
    public Vector3 feedingOffset; //relative position of player when being fed on
    public Vector3 feedingRotationOffset; //relative position of player when being fed on

    public bool CanSeePlayer()
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

    public virtual void StartStruggle()
    {
        anim.SetBool("Feeding", true);
    }

    public virtual void EndStruggle()
    {
        anim.SetBool("Feeding", false);
    }
}