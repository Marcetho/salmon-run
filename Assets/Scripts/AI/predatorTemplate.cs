using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class predatorTemplate : MonoBehaviour
{
    public float detectionRadius = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    public GameObject player;
    public Animator animator;
    public GameController gameController;
    private NavMeshAgent agent;
    private bool canAttack = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        player = GameController.currentPlayer;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        if (distanceToPlayer <= detectionRadius)
        {
            if (CanSeePlayer())
            {
                Debug.Log("Player detected!");
                agent.SetDestination(player.transform.position);

                if (distanceToPlayer <= attackRange && canAttack)
                {
        
                    gameController.OnPlayerDamaged(25);

                    
                    StartCoroutine(GrabAttack());
                }
            }
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

    IEnumerator GrabAttack()
    {
        canAttack = false;
        agent.isStopped = true;
        animator.SetTrigger("GrabAttack");


        yield return new WaitForSeconds(attackCooldown);
        
        agent.isStopped = false;
        canAttack = true;
    }
}
