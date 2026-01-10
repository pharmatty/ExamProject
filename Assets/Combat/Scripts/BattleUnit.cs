using UnityEngine;
using System.Collections;
using DamageNumbersPro;

public class BattleUnit : MonoBehaviour
{
    [Header("Character Reference (Player)")]
    public CharacterData characterData;

    [Header("Character Reference (Enemy)")]
    public EnemyData enemyData;

    [Header("Runtime Stats")]
    public int currentHealth;
    public int currentAP;

    [Header("Battle Flags")]
    public bool isEnemy;
    public bool IsDead => currentHealth <= 0;

    [Header("Spawn Info")]
    public int spawnIndex = -1;

    [Header("References")]
    public Animator animator;
    public Transform cameraFocusPoint;

    [Header("Damage / Heal Numbers")]
    public DamageNumber damageNumberPrefab;
    public DamageNumber healNumberPrefab;
    public Transform damageNumberAnchor;

    private Transform visualRoot;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private BattleUIManager uiManager;

    private static readonly int ATTACK_TRIGGER = Animator.StringToHash("Attack");
    private static readonly int HIT_TRIGGER    = Animator.StringToHash("Hit");
    private static readonly int DEATH_TRIGGER  = Animator.StringToHash("Death");

    private const string ATTACK_STATE_NAME = "Attack02";

    private const int DEFAULT_MAX_AP = 6;
    private const int DEFAULT_START_AP = 1;

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

            if (characterData.maxAP <= 0)
                characterData.maxAP = DEFAULT_MAX_AP;

            if (characterData.currentAP < 0 || characterData.currentAP > characterData.maxAP)
                characterData.currentAP = DEFAULT_START_AP;

            
            currentAP = characterData.currentAP;
            characterData.currentAP = currentAP;
        }
        else
        {
            this.enemyData = enemyData;
            currentHealth = enemyData.maxHealth;
            currentAP = 0;
        }

        BindAnimator();
        CacheStartTransform();

        if (!isEnemy && uiManager != null)
        {
            uiManager.UpdateHealth(currentHealth, characterData.maxHealth);
            uiManager.UpdateAP(currentAP, characterData.maxAP);
        }
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

   
    public bool CanAffordAP(int cost) => currentAP >= cost;

    public void SpendAP(int cost)
    {
        if (!isEnemy && characterData == null) return;

        currentAP = Mathf.Max(0, currentAP - cost);

        if (!isEnemy)
            characterData.currentAP = currentAP;

        uiManager ??= FindFirstObjectByType<BattleUIManager>();
        if (uiManager != null && !isEnemy)
            uiManager.UpdateAP(currentAP, characterData.maxAP);
    }

    public void GainAP(int amount)
    {
        if (!isEnemy && characterData == null) return;
        if (isEnemy) return;

        currentAP = Mathf.Clamp(currentAP + amount, 0, characterData.maxAP);
        characterData.currentAP = currentAP;

        uiManager ??= FindFirstObjectByType<BattleUIManager>();
        if (uiManager != null)
            uiManager.UpdateAP(currentAP, characterData.maxAP);
    }

    // Attack sequence setup //MN
    public IEnumerator PerformAttack(BattleUnit target)
    {
        if (target == null || target.IsDead || animator == null || visualRoot == null)
            yield break;

        Vector3 dir = (target.transform.position - visualRoot.position).normalized;
        Vector3 attackPos = target.transform.position - dir * 1.5f;

        yield return MoveTo(attackPos, 0.15f);
        FaceTarget(target.transform.position);

        animator.ResetTrigger(ATTACK_TRIGGER);
        animator.SetTrigger(ATTACK_TRIGGER);

        yield return WaitUntilStateEntered(ATTACK_STATE_NAME, 1.0f);

        yield return new WaitForSeconds(0.35f);
        target.ReceiveAttack(this);

        yield return WaitUntilStateFinished(ATTACK_STATE_NAME, 3.0f);

        GainAP(1);

        yield return MoveTo(startPosition, 0.2f);
        visualRoot.rotation = startRotation;
    }

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

    // Damage numbers, call in// MN
    private Transform GetDamageAnchor()
    {
        if (damageNumberAnchor != null) return damageNumberAnchor;
        if (cameraFocusPoint != null) return cameraFocusPoint;
        return transform;
    }

    private void ShowDamageNumber(int amount)
    {
        if (damageNumberPrefab == null || amount == 0)
            return;

        Transform anchor = GetDamageAnchor();
        DamageNumber dn = damageNumberPrefab.Spawn(anchor.position, amount);
        if (dn != null)
            dn.followedTarget = anchor;
    }

    public void ShowHealNumber(int amount)
    {
        if (healNumberPrefab == null || amount == 0)
            return;

        Transform anchor = GetDamageAnchor();
        DamageNumber dn = healNumberPrefab.Spawn(anchor.position, amount);
        if (dn != null)
            dn.followedTarget = anchor;
    }

    // Damage, defence and crit variation //MN
    public int CalculateDamageFrom(BattleUnit attacker)
    {
        int atk = attacker.isEnemy ? attacker.enemyData.attack : attacker.characterData.attack;
        int def = isEnemy ? enemyData.defense : characterData.defense;

        int baseDamage = Mathf.Max(1, atk - def);

        float variance = Random.Range(0.80f, 1.20f);
        int varied = Mathf.RoundToInt(baseDamage * variance);

        bool crit = Random.value < 0.10f;
        if (crit)
            varied = Mathf.RoundToInt(varied * 1.5f);

        return Mathf.Max(1, varied);
    }

    public void ReceiveAttack(BattleUnit attacker)
    {
        int dmg = CalculateDamageFrom(attacker);
        currentHealth = Mathf.Max(0, currentHealth - dmg);

        if (dmg > 0)
            ShowDamageNumber(dmg);

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

    // Here's the movement setup /MN
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
}
