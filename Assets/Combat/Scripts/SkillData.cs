using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Game Data/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public int skillPointCost;
    public int power;
    public Sprite icon; // optional
}