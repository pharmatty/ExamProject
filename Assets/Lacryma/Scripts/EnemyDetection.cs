using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    private MovementInput movementInput;
    private CombatScript combatScript;

    public LayerMask layerMask;

    [SerializeField] Vector3 inputDirection;
    [SerializeField] private EnemyScript currentTarget;

    public GameObject cam;   // not actually used, but here for parity

    private void Start()
    {
        movementInput = GetComponentInParent<MovementInput>();
        combatScript  = GetComponentInParent<CombatScript>();
    }

    private void Update()
    {
        // 1. Camera-relative input direction
        var camera = Camera.main;
        var forward = camera.transform.forward;
        var right   = camera.transform.right;

        forward.y = 0f;
        right.y   = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection =
            forward * movementInput.moveAxis.y +
            right   * movementInput.moveAxis.x;

        inputDirection = inputDirection.normalized;

        // 2. SphereCast in that direction to find an enemy
        RaycastHit info;
        if (Physics.SphereCast(transform.position,
                               3f,
                               inputDirection,
                               out info,
                               10f,
                               layerMask))
        {
            EnemyScript enemy = info.collider.transform.GetComponent<EnemyScript>();
            if (enemy != null && enemy.IsAttackable())
                currentTarget = enemy;
        }
    }

    public EnemyScript CurrentTarget()
    {
        return currentTarget;
    }

    public void SetCurrentTarget(EnemyScript target)
    {
        currentTarget = target;
    }

    public float InputMagnitude()
    {
        return inputDirection.magnitude;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, inputDirection * 3f);
        Gizmos.DrawWireSphere(transform.position, 1);
        if (CurrentTarget() != null)
            Gizmos.DrawSphere(CurrentTarget().transform.position, 0.5f);
    }
}
