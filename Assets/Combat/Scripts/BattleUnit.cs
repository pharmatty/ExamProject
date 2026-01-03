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
    public int spawnIndex = -1;

    [Header("References")]
    public Animator animator;
    public Transform cameraFocusPoint;

    private Transform visualRoot;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private BattleUIManager uiManager;

    private static readonly int ATTACK_TRIGGER = Animator.StringToHash("Attack");
    private static readonly int HIT_TRIGGER    = Animator.StringToHash("Hit");
    private static readonly int DEATH_TRIGGER  = Animator.StringToHash("Death");

    private const string ATTACK_STATE_NAME = "Attack02";

    private const int MIN_SP = 1;
    private const int MAX_SP = 6;

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
            // PLAYER
            characterData = charData;
            currentHealth = characterData.currentHealth;

            characterData.maxSkillPoints = MAX_SP;

            // 👉 ALWAYS START COMBAT WITH EXACTLY 1 SP
            currentSkillPoints = MIN_SP;
            characterData.currentSkillPoints = MIN_SP;
        }
        else
        {
            // ENEMY
            this.enemyData = enemyData;
            currentHealth = enemyData.maxHealth;
            currentSkillPoints = 0;
        }

        BindAnimator();
        CacheStartTransform();

        if (!isEnemy && uiManager != null)
            uiManager.UpdateSP(currentSkillPoints, MAX_SP);
    }

    private void BindAnimator()
    {
        animator = GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            Debug.LogError("[BattleUnit] No Animator found on " + name);
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
    // SP SYSTEM
    // =========================
    public bool CanAffordSP(int cost) =>
        (currentSkillPoints - cost) >= MIN_SP;

    public void SpendSP(int cost)
    {
        if (!isEnemy && characterData == null) return;

        currentSkillPoints = Mathf.Max(MIN_SP, currentSkillPoints - cost);

        if (!isEnemy)
            characterData.currentSkillPoints = currentSkillPoints;

        uiManager ??= FindFirstObjectByType<BattleUIManager>();
        if (uiManager != null && !isEnemy)
            uiManager.UpdateSP(currentSkillPoints, MAX_SP);
    }

    public void GainSP(int amount)
    {
        if (!isEnemy && characterData == null) return;

        currentSkillPoints =
            Mathf.Clamp(currentSkillPoints + amount, MIN_SP, MAX_SP);

        if (!isEnemy)
            characterData.currentSkillPoints = currentSkillPoints;

        uiManager ??= FindFirstObjectByType<BattleUIManager>();
        if (uiManager != null && !isEnemy)
            uiManager.UpdateSP(currentSkillPoints, MAX_SP);
    }

    // =========================
    // ATTACK (RESTORED TIMING)
    // =========================
    public IEnumerator PerformAttack(BattleUnit target)
    {
        if (target == null || target.IsDead || animator == null || visualRoot == null)
            yield break;

        Vector3 dir = (target.transform.position - visualRoot.position).normalized;
        Vector3 attackPos = target.transform.position - dir * 1.5f;

        // Move in
        yield return MoveTo(attackPos, 0.15f);
        FaceTarget(target.transform.position);

        animator.ResetTrigger(ATTACK_TRIGGER);
        animator.SetTrigger(ATTACK_TRIGGER);

        // Wait until animation actually starts
        yield return WaitUntilStateEntered(ATTACK_STATE_NAME, 1.0f);

        // Hit timing window
        yield return new WaitForSeconds(0.35f);
        target.ReceiveAttack(this);

        // Wait until animation finishes
        yield return WaitUntilStateFinished(ATTACK_STATE_NAME, 3.0f);

        // Gain SP on attack
        GainSP(1);

        // Return to spawn
        yield return MoveTo(startPosition, 0.2f);
        visualRoot.rotation = startRotation;
    }

    // =========================
    // SKILL (same behavior pattern)
    // =========================
    public IEnumerator PerformSkill(BattleUnit target)
    {
        if (target == null || target.IsDead || animator == null || visualRoot == null)
            yield break;

        Vector3 dir = (target.transform.position - visualRoot.position).normalized;
        Vector3 pos = target.transform.position - dir * 1.3f;

        yield return MoveTo(pos, 0.18f);
        FaceTarget(target.transform.position);

        animator.ResetTrigger("Skill1");
        animator.SetTrigger("Skill1");

        yield return new WaitForSeconds(0.35f);
        target.ReceiveAttack(this);

        yield return MoveTo(startPosition, 0.2f);
        visualRoot.rotation = startRotation;
    }

    // =========================
    // ANIMATION HELPERS (RESTORED)
    // =========================
    private IEnumerator WaitUntilStateEntered(string state, float timeout)
    {
        float t = 0f;

        while (t < timeout)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(state))
                yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitUntilStateFinished(string state, float timeout)
    {
        float t = 0f;

        while (t < timeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName(state) && info.normalizedTime >= 1f)
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
        int atk = attacker.isEnemy ? attacker.enemyData.attack : attacker.characterData.attack;
        int def = isEnemy ? enemyData.defense : characterData.defense;

        return Mathf.Max(1, atk - def);
    }

    public void ReceiveAttack(BattleUnit attacker)
    {
        int dmg = CalculateDamageFrom(attacker);
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        animator?.SetTrigger(HIT_TRIGGER);

        if (!isEnemy)
        {
            characterData.currentHealth = currentHealth;

            GameManager.Instance?.SavePlayerHealth(
                currentHealth,
                characterData.maxHealth
            );

            uiManager?.UpdateHealth(
                currentHealth,
                characterData.maxHealth
            );
        }

        if (currentHealth <= 0)
            animator?.SetTrigger(DEATH_TRIGGER);
    }

    // =========================
    // MOVEMENT + FACING
    // =========================
    private IEnumerator MoveTo(Vector3 dest, float duration)
    {
        Vector3 start = visualRoot.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            visualRoot.position = Vector3.Lerp(start, dest, t / duration);
            yield return null;
        }

        visualRoot.position = dest;
    }

    private void FaceTarget(Vector3 pos)
    {
        Vector3 dir = pos - visualRoot.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
            visualRoot.rotation = Quaternion.LookRotation(dir);
    }
}
