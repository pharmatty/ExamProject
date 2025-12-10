using UnityEngine;

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
    public bool isEnemy;   // true = enemy, false = player

    [Header("References")]
    public Animator animator;
    public Transform cameraFocusPoint;

    public bool IsDead => currentHealth <= 0;

    /// <summary>
    /// Initializes this BattleUnit with either CharacterData (player)
    /// or EnemyData (enemy).
    /// </summary>
    public void Initialize(CharacterData charData = null, EnemyData enemyData = null, bool enemy = false)
    {
        isEnemy = enemy;

        if (!enemy)
        {
            // Player unit
            characterData = charData;

            currentHealth = characterData.currentHealth;
            currentSkillPoints = characterData.currentSkillPoints;
        }
        else
        {
            // Enemy unit
            this.enemyData = enemyData;

            currentHealth = enemyData.maxHealth;
            currentSkillPoints = 0; // enemies may not use SP unless desired
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
    }

    public void UseSkillPoints(int amount)
    {
        currentSkillPoints = Mathf.Max(0, currentSkillPoints - amount);
    }
}