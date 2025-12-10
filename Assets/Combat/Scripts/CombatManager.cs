using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] playerSpawnPoints;   // usually 1 for now
    public Transform[] enemySpawnPoints;    // 3 positions for enemies

    [Header("Prefabs")]
    public GameObject playerBattleUnitPrefab;

    [Header("Enemy Database")]
    public List<EnemyData> possibleEnemies = new List<EnemyData>();

    [Header("Runtime")]
    public List<BattleUnit> playerUnits = new List<BattleUnit>();
    public List<BattleUnit> enemyUnits = new List<BattleUnit>();

    void Start()
    {
        SpawnPlayerUnits();
        SpawnEnemyUnits();
    }

    void SpawnPlayerUnits()
    {
        var party = GameManager.Instance.partyData.currentParty;

        for (int i = 0; i < party.Count && i < playerSpawnPoints.Length; i++)
        {
            CharacterData data = party[i];

            GameObject obj = Instantiate(playerBattleUnitPrefab,
                playerSpawnPoints[i].position,
                Quaternion.identity);

            BattleUnit unit = obj.GetComponent<BattleUnit>();
            unit.Initialize(charData: data, enemy: false);

            playerUnits.Add(unit);
        }
    }

    void SpawnEnemyUnits()
    {
        // Pick a random number of enemies (1 to 3)
        int enemyCount = Random.Range(1, 4);

        for (int i = 0; i < enemyCount && i < enemySpawnPoints.Length; i++)
        {
            EnemyData chosen = possibleEnemies[Random.Range(0, possibleEnemies.Count)];

            GameObject obj = Instantiate(chosen.enemyPrefab,
                enemySpawnPoints[i].position,
                Quaternion.identity);

            BattleUnit unit = obj.GetComponent<BattleUnit>();
            unit.Initialize(enemyData: chosen, enemy: true);

            enemyUnits.Add(unit);
        }
    }
}