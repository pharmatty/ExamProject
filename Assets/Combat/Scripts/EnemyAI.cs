using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private BattleUnit unit;

    private void Awake()
    {
        unit = GetComponent<BattleUnit>();
    }

    /// <summary>
    /// Called by CombatManager when it is this enemy's turn.
    /// </summary>
    public IEnumerator TakeTurn(BattleUnit target)
    {
        if (unit == null || unit.IsDead)
            yield break;

        // Simple delay for readability (Persona-style "thinking" beat)
        yield return new WaitForSeconds(Random.Range(0.4f, 0.8f));

        // Perform basic attack
        yield return unit.PerformAttack(target);
    }
}