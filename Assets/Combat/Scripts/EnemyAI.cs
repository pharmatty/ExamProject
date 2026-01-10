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
    /// Called by CombatManager when it is this enemy's turn. /MN
    /// </summary>
    public IEnumerator TakeTurn(BattleUnit target)
    {
        if (unit == null || unit.IsDead)
            yield break;

        
        yield return new WaitForSeconds(Random.Range(0.4f, 0.8f));

        
        yield return unit.PerformAttack(target); //Future possible commands need to adhere to different enemy types /MN
    }
}