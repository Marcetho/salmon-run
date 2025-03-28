using UnityEngine;
using System;

public class PredatorAI : MonoBehaviour
{
    protected GameObject player;
    protected PlayerMovement playerMove;
    protected Animator anim;

    [Header("Stats")]
    [SerializeField] protected Collider bodyCollider;
    [SerializeField] protected float detectionRadius = 20f;
    [SerializeField] protected float attackCooldown = 1f; //dmg interval (in seconds) during struggle
    [SerializeField] protected int attackDmg = 25; //dmg per interval
    [SerializeField] protected bool canAttack = true; // if can currently attack
    [Header("Visual")]
    [SerializeField] protected Vector3 feedingOffset; //relative position of player when being fed on
    [SerializeField] protected Vector3 feedingRotationOffset; //relative position of player when being fed on

    public float AttackCooldown => attackCooldown;
    public int AttackDmg => attackDmg;
    public Vector3 FeedingOffset => feedingOffset;
    public Vector3 FeedingRotationOffset => feedingRotationOffset;
    public bool CanAttack => canAttack;

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

    public virtual void EndStruggle(bool success)
    {
        anim.SetBool("Feeding", false);
    }
}