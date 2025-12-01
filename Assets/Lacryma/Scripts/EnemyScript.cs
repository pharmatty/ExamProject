using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class EnemyScript : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    private EnemyManager enemyManager;
    private CombatScript combat;
    private EnemyDetection detection;

    private Transform playerTransform;

    [Header("Stats")]
    public int health = 3;

    [Header("States")]
    public bool preparingAttack;
    public bool moving;
    public bool retreating;
    public bool lockedTarget;
    public bool stunned;
    public bool idleState = true;

    [Header("FX")]
    public ParticleSystem counterParticle;

    Coroutine moveCo;

    public UnityEvent<EnemyScript> OnDamage;
    public UnityEvent<EnemyScript> OnStopMoving;
    public UnityEvent<EnemyScript> OnRetreat;


    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        enemyManager = GetComponentInParent<EnemyManager>();
    }


    void Start()
    {
        combat = FindFirstObjectByType<CombatScript>();
        detection = combat.GetComponentInChildren<EnemyDetection>();

        playerTransform = combat.transform;

        combat.OnHit.AddListener(OnPlayerHit);
        combat.OnTrajectory.AddListener(OnPlayerTrajectory);
        combat.OnCounterAttack.AddListener(OnPlayerCounter);

        moveCo = StartCoroutine(Movement());
    }


    void Update()
    {
        if (playerTransform == null || stunned) return;

        Vector3 look = playerTransform.position;
        look.y = transform.position.y;
        transform.LookAt(look);

        if (moving)
            MoveEnemy();
    }


    // ---------------- HIT ----------------
    void OnPlayerHit(EnemyScript target)
    {
        if (target != this) return;

        stunned = true;
        health--;

        animator.SetTrigger("Hit");

        transform.DOMove(transform.position - transform.forward * 0.5f, 0.2f);

        OnDamage.Invoke(this);

        if (health <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(Recover());
    }

    IEnumerator Recover()
    {
        yield return new WaitForSeconds(0.4f);
        stunned = false;
    }


    // ---------------- TRAJECTORY ----------------
    void OnPlayerTrajectory(EnemyScript target)
    {
        if (target != this) return;

        lockedTarget = true;

        moving = false;
        preparingAttack = false;
    }


    // ---------------- COUNTER ----------------
    void OnPlayerCounter(EnemyScript target)
    {
        if (target == this)
            preparingAttack = false;
    }


    // ---------------- DEATH ----------------
    void Die()
    {
        stunned = true;
        moving = false;
        preparingAttack = false;

        animator.SetTrigger("Death");
        controller.enabled = false;

        enemyManager.SetEnemyAvailability(this, false);
    }


    // ---------------- ATTACK ----------------
    public void SetAttack()
    {
        preparingAttack = true;
        counterParticle?.Play();

        moving = true;
    }


    // ---------------- RETREAT ----------------
    public void SetRetreat()
    {
        retreating = true;
        moving = true;
        StartCoroutine(Retreat());
    }


    IEnumerator Retreat()
    {
        yield return new WaitForSeconds(1.4f);

        OnRetreat.Invoke(this);

        while (Vector3.Distance(transform.position, playerTransform.position) < 4f)
        {
            controller.Move(-transform.forward * 2f * Time.deltaTime);
            yield return null;
        }

        retreating = false;
        moving = false;
        idleState = true;
        moveCo = StartCoroutine(Movement());
    }


    // ---------------- MOVEMENT ----------------
    IEnumerator Movement()
    {
        while (idleState)
        {
            if (Random.value > 0.5f)
                moving = true;
            else
                moving = false;

            yield return new WaitForSeconds(1f);
        }
    }


    void MoveEnemy()
    {
        if (stunned || retreating) return;

        controller.Move(transform.forward * 2f * Time.deltaTime);
        animator.SetFloat("InputMagnitude", 1, 0.1f, Time.deltaTime);
    }


    public void StopMoving()
    {
        moving = false;
        animator.SetFloat("InputMagnitude", 0);
        OnStopMoving.Invoke(this);
    }


    // ---------------- GETTERS ----------------
    public bool IsPreparingAttack() => preparingAttack;
    public bool IsRetreating() => retreating;
    public bool IsLockedTarget() => lockedTarget;
    public bool IsStunned() => stunned;
    public bool IsAttackable() => !stunned && health > 0;
}
