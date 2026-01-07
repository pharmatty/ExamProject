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

        startPosition = transform.position; // remember spawn point

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

        // --- Player in range → chase ---
        if (inRange)
        {
            returningHome = false;
            timeOutOfRange = 0f;

            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // Count time outside chase range
            timeOutOfRange += Time.deltaTime;

            // After 3 seconds → return to start
            if (timeOutOfRange >= 3f && !returningHome)
            {
                returningHome = true;
                agent.isStopped = false;
                agent.SetDestination(startPosition);
            }

            // When back at origin → stop + idle
            if (returningHome && Vector3.Distance(transform.position, startPosition) < 0.2f)
            {
                agent.isStopped = true;
                returningHome = false;
            }
        }

        // --- Animation ---
        bool isMoving = agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool("IsWalking", isMoving);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
