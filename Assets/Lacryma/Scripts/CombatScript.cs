using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class CombatScript : MonoBehaviour
{
    // References
    private EnemyManager enemyManager;
    private EnemyDetection enemyDetection;
    private MovementInput movementInput;
    private Animator animator;

    // This is the actual object we move with DOTween
    private Transform playerTransform;

    [Header("Target")]
    private EnemyScript lockedTarget;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 0.6f;

    [Header("States")]
    public bool isAttackingEnemy = false;
    public bool isCountering = false;

    [Header("Public References")]
    [SerializeField] private Transform punchPosition;
    [SerializeField] private ParticleSystemScript punchParticle;
    [SerializeField] private GameObject lastHitCamera;
    [SerializeField] private Transform lastHitFocusObject;

    // Events
    public UnityEvent<EnemyScript> OnTrajectory;
    public UnityEvent<EnemyScript> OnHit;
    public UnityEvent<EnemyScript> OnCounterAttack;

    // Internal
    private Coroutine attackCoroutine;
    private Coroutine counterCoroutine;

    private int comboIndex = 0;
    private readonly string[] attackAnimations =
        { "AirKick", "AirKick2", "AirPunch", "AirKick3" };


    void Awake()
    {
        enemyManager = FindFirstObjectByType<EnemyManager>();

        movementInput = GetComponentInParent<MovementInput>();
        if (movementInput == null)
        {
            Debug.LogError("CombatScript: MovementInput not found.");
            return;
        }

        playerTransform = movementInput.transform;
        animator = playerTransform.GetComponent<Animator>();

        enemyDetection = playerTransform.GetComponentInChildren<EnemyDetection>();
    }


    // ------------------ INPUT ------------------
    public void OnAttack() => AttackCheck();
    public void OnCounter() => CounterCheck();


    // ------------------ ATTACK CHECK ------------------
    void AttackCheck()
    {
        if (isAttackingEnemy || isCountering)
            return;

        EnemyScript target = enemyDetection.CurrentTarget();

        if (target == null)
        {
            if (enemyManager.AliveEnemyCount() == 0)
            {
                Attack(null, 0);
                return;
            }
            target = enemyManager.RandomEnemy();
        }

        if (enemyDetection.InputMagnitude() > 0.2f)
            target = enemyDetection.CurrentTarget();

        lockedTarget = target ?? enemyManager.RandomEnemy();

        Attack(lockedTarget, TargetDistance(lockedTarget));
    }


    // ------------------ ATTACK EXECUTION ------------------
    public void Attack(EnemyScript target, float distance)
    {
        if (target == null)
        {
            PlayAttack("GroundPunch");
            return;
        }

        string anim;

        if (distance < 15f)
        {
            comboIndex = (comboIndex + 1) % attackAnimations.Length;
            anim = attackAnimations[comboIndex];
        }
        else
        {
            lockedTarget = null;
            anim = "GroundPunch";
        }

        PlayAttack(anim, target);
    }


    void PlayAttack(string trigger, EnemyScript target = null)
    {
        animator.SetTrigger(trigger);

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackRoutine(trigger.Contains("Air") ? attackCooldown : 0.3f));

        if (target == null)
            return;

        target.StopMoving();
        MoveTowardsTarget(target);
    }


    IEnumerator AttackRoutine(float duration)
    {
        isAttackingEnemy = true;

        movementInput.enabled = false;
        movementInput.acceleration = 0;

        yield return new WaitForSeconds(duration);

        isAttackingEnemy = false;

        yield return new WaitForSeconds(0.15f);

        movementInput.enabled = true;
        movementInput.acceleration = 1;
    }


    // ------------------ MOVEMENT TO TARGET ------------------
    void MoveTowardsTarget(EnemyScript target)
    {
        OnTrajectory.Invoke(target);

        playerTransform.DOLookAt(target.transform.position, 0.15f);

        Vector3 destination = TargetOffset(target.transform);

        // THIS is what worked in your simplified script
        playerTransform.DOMove(destination, 0.65f)
                       .SetEase(Ease.OutQuad);
    }


    // ------------------ FINAL BLOW ------------------
    IEnumerator FinalBlowRoutine()
    {
        Time.timeScale = 0.5f;

        if (lastHitCamera != null)
            lastHitCamera.SetActive(true);

        if (lastHitFocusObject != null && lockedTarget != null)
            lastHitFocusObject.position = lockedTarget.transform.position;

        yield return new WaitForSecondsRealtime(2f);

        if (lastHitCamera != null)
            lastHitCamera.SetActive(false);

        Time.timeScale = 1f;
    }


    // ------------------ COUNTER SYSTEM ------------------
    void CounterCheck()
    {
        if (isCountering || isAttackingEnemy || !enemyManager.AnEnemyIsPreparingAttack())
            return;

        lockedTarget = ClosestCounterEnemy();
        if (lockedTarget == null)
            return;

        OnCounterAttack.Invoke(lockedTarget);

        float dist = TargetDistance(lockedTarget);

        if (dist > 2f)
        {
            Attack(lockedTarget, dist);
            return;
        }

        animator.SetTrigger("Dodge");
        playerTransform.DOLookAt(lockedTarget.transform.position, 0.2f);
        playerTransform.DOMove(playerTransform.position + lockedTarget.transform.forward, 0.2f);

        if (counterCoroutine != null)
            StopCoroutine(counterCoroutine);

        counterCoroutine = StartCoroutine(CounterRoutine());
    }


    IEnumerator CounterRoutine()
    {
        isCountering = true;
        movementInput.enabled = false;

        yield return new WaitForSeconds(0.2f);

        isCountering = false;
        movementInput.enabled = true;

        if (lockedTarget != null)
            Attack(lockedTarget, TargetDistance(lockedTarget));
    }


    // ------------------ HELPERS ------------------
    EnemyScript ClosestCounterEnemy()
    {
        EnemyScript[] list = FindObjectsByType<EnemyScript>(FindObjectsSortMode.None);

        float min = Mathf.Infinity;
        EnemyScript closest = null;

        foreach (var e in list)
        {
            if (!e.IsPreparingAttack())
                continue;

            float dist = TargetDistance(e);
            if (dist < min)
            {
                min = dist;
                closest = e;
            }
        }

        return closest;
    }


    float TargetDistance(EnemyScript enemy)
    {
        if (enemy == null) return 999f;
        return Vector3.Distance(playerTransform.position, enemy.transform.position);
    }


    Vector3 TargetOffset(Transform t)
    {
        return Vector3.MoveTowards(t.position, playerTransform.position, 0.95f);
    }


    public void HitEvent()
    {
        if (lockedTarget == null)
            return;

        OnHit.Invoke(lockedTarget);

        if (punchParticle != null && punchPosition != null)
            punchParticle.PlayParticleAtPosition(punchPosition.position);
    }
}
