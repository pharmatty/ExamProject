using UnityEngine;
using System.Collections;

public class BattleUnit : MonoBehaviour
{
    [Header("Character Reference (Player)")]
    public CharacterData characterData;

    [Header("Character Reference (Enemy)")]
    public EnemyData enemyData;

    [Header("Runtime Stats")]
    public int currentHealth;
    public int currentSkillPoints;

    [Header("Battle Flags")]
    public bool isEnemy;
    public bool IsDead => currentHealth <= 0;

    [Header("Spawn Info")]
    public int spawnIndex = -1; // ✅ NEW (stable mapping)

    [Header("References")]
    public Animator animator;
    public Transform cameraFocusPoint;

    private Transform visualRoot;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private BattleUIManager uiManager;

    private static readonly int ATTACK_TRIGGER = Animator.StringToHash("Attack");
    private static readonly int HIT_TRIGGER = Animator.StringToHash("Hit");
    private static readonly int DEATH_TRIGGER = Animator.StringToHash("Death");

    private const string ATTACK_STATE_NAME = "Attack02";

    private void Awake()
    {
        BindAnimator();
        CacheStartTransform();
        uiManager = FindFirstObjectByType<BattleUIManager>();
    }

    public void Initialize(CharacterData charData = null, EnemyData enemyData = null, bool enemy = false)
    {
        isEnemy = enemy;

        if (!enemy)
        {
            characterData = charData;
            currentHealth = characterData.currentHealth;
            currentSkillPoints = characterData.currentSkillPoints;
        }
        else
        {
            this.enemyData = enemyData;
            currentHealth = enemyData.maxHealth;
            currentSkillPoints = 0;
        }

        BindAnimator();
        CacheStartTransform();
    }

    private void BindAnimator()
    {
        animator = GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError($"[BattleUnit] No Animator found on {name}");
            return;
        }

        visualRoot = animator.transform;
    }

    private void CacheStartTransform()
    {
        if (visualRoot == null) return;
        startPosition = visualRoot.position;
        startRotation = visualRoot.rotation;
    }

    // =========================
    // ATTACK SEQUENCE
    // =========================
    public IEnumerator PerformAttack(BattleUnit target)
    {
        if (target == null || target.IsDead || animator == null || visualRoot == null)
            yield break;

        Vector3 directionToTarget =
            (target.transform.position - visualRoot.position).normalized;

        Vector3 attackPosition =
            target.transform.position -
            directionToTarget * 1.5f;

        yield return MoveTo(attackPosition, 0.15f);
        FaceTarget(target.transform.position);

        animator.ResetTrigger(ATTACK_TRIGGER);
        animator.SetTrigger(ATTACK_TRIGGER);

        yield return WaitUntilStateEntered(ATTACK_STATE_NAME, 1.0f);
        yield return new WaitForSeconds(0.35f);

        target.ReceiveAttack(this);

        yield return WaitUntilStateFinished(ATTACK_STATE_NAME, 3.0f);

        yield return MoveTo(startPosition, 0.2f);
        visualRoot.rotation = startRotation;
    }

    private IEnumerator WaitUntilStateEntered(string stateName, float timeoutSeconds)
    {
        float t = 0f;
        while (t < timeoutSeconds)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitUntilStateFinished(string stateName, float timeoutSeconds)
    {
        float t = 0f;
        while (t < timeoutSeconds)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName) && info.normalizedTime >= 1f)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    // =========================
    // DAMAGE
    // =========================
    public int CalculateDamageFrom(BattleUnit attacker)
    {
        int atk = attacker.isEnemy
            ? attacker.enemyData.attack
            : attacker.characterData.attack;

        int def = isEnemy
            ? enemyData.defense
            : characterData.defense;

        return Mathf.Max(1, atk - def);
    }

    public void ReceiveAttack(BattleUnit attacker)
    {
        int damage = CalculateDamageFrom(attacker);
        currentHealth = Mathf.Max(0, currentHealth - damage);

        animator?.SetTrigger(HIT_TRIGGER);

        if (!isEnemy)
        {
            characterData.currentHealth = currentHealth;

            GameManager.Instance?.SavePlayerHealth(
                characterData.currentHealth,
                characterData.maxHealth
            );

            uiManager?.UpdateHealth(
                characterData.currentHealth,
                characterData.maxHealth
            );
        }

        if (currentHealth <= 0)
            animator?.SetTrigger(DEATH_TRIGGER);
    }

    private IEnumerator MoveTo(Vector3 destination, float duration)
    {
        Vector3 start = visualRoot.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            visualRoot.position = Vector3.Lerp(start, destination, elapsed / duration);
            yield return null;
        }

        visualRoot.position = destination;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - visualRoot.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
            visualRoot.rotation = Quaternion.LookRotation(dir);
    }
}
