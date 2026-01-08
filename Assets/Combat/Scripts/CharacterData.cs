using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game Data/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Lacryma";

    [Header("Core Stats")]
    public int level = 1;

    public int maxHealth = 100;
    public int currentHealth = 100;

    // AP (used to be Skill Points)
    public int maxAP = 6;
    public int currentAP = 1;

    [Header("Combat Stats")]
    public int attack = 10;
    public int defense = 5;
    public int speed = 8;

    [Header("Skills")]
    public List<SkillData> learnedSkills = new List<SkillData>();

    [Header("Items")]
    public List<ItemData> carriedItems = new List<ItemData>();

    public void ResetStatsForBattle()
    {
        currentHealth = maxHealth;
        currentAP = Mathf.Clamp(currentAP, 0, maxAP);
    }
}