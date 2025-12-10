using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;

    [Header("Stats")]
    public int maxHealth = 50;
    public int attack = 8;
    public int defense = 3;
    public int speed = 5;

    [Header("Skills")]
    public List<SkillData> enemySkills = new List<SkillData>();

    [Header("Rewards")]
    public int expReward = 10;
    public int goldReward = 5;

    [Header("Visuals")]
    public GameObject enemyPrefab; // Model or prefab for the battle scene
}