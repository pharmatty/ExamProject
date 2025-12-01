using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public EnemyStruct[] allEnemies;
    private Coroutine aiLoop;

    void Start()
    {
        EnemyScript[] list = GetComponentsInChildren<EnemyScript>();
        allEnemies = new EnemyStruct[list.Length];

        for (int i = 0; i < list.Length; i++)
        {
            allEnemies[i].enemyScript = list[i];
            allEnemies[i].enemyAvailability = true;
        }

        aiLoop = StartCoroutine(AILoop());
    }

    IEnumerator AILoop()
    {
        while (AliveEnemyCount() > 0)
        {
            yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));

            EnemyScript e = RandomEnemy();
            if (e == null) continue;

            e.SetAttack();

            yield return new WaitUntil(() => !e.IsPreparingAttack());

            e.SetRetreat();
        }
    }

    public EnemyScript RandomEnemy()
    {
        List<EnemyScript> list = new();

        foreach (var e in allEnemies)
            if (e.enemyAvailability)
                list.Add(e.enemyScript);

        if (list.Count == 0) return null;

        return list[Random.Range(0, list.Count)];
    }

    public int AliveEnemyCount()
    {
        int count = 0;
        foreach (var e in allEnemies)
            if (e.enemyAvailability && e.enemyScript != null)
                count++;

        return count;
    }

    public void SetEnemyAvailability(EnemyScript enemy, bool state)
    {
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i].enemyScript == enemy)
                allEnemies[i].enemyAvailability = state;
        }

        EnemyDetection detect = FindFirstObjectByType<EnemyDetection>();

        if (detect != null && detect.CurrentTarget() == enemy)
            detect.SetCurrentTarget(null);
    }

    public bool AnEnemyIsPreparingAttack()
    {
        foreach (var e in allEnemies)
            if (e.enemyScript.IsPreparingAttack())
                return true;

        return false;
    }
}

[System.Serializable]
public struct EnemyStruct
{
    public EnemyScript enemyScript;
    public bool enemyAvailability;
}
