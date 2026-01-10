using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public Transform player;
    public float chaseRange = 8f;
    public float stopDistance = 1.5f;

    private NavMeshAgent agent;
    private Animator animator;

    private Vector3 startPosition;
    private bool returningHome = false;
    private float timeOutOfRange = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        startPosition = transform.position; 

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);
        bool inRange = distance <= chaseRange && distance > stopDistance;

        
        if (inRange)
        {
            returningHome = false;
            timeOutOfRange = 0f;

            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
           
            timeOutOfRange += Time.deltaTime;

            
            if (timeOutOfRange >= 3f && !returningHome)
            {
                returningHome = true;
                agent.isStopped = false;
                agent.SetDestination(startPosition);
            }

           
            if (returningHome && Vector3.Distance(transform.position, startPosition) < 0.2f)
            {
                agent.isStopped = true;
                returningHome = false;
            }
        }

        
        bool isMoving = agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool("IsWalking", isMoving);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
