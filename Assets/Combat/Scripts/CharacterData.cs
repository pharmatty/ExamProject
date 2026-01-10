using UnityEngine;

[CreateAssetMenu(menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;

    [Header("Health")]
    public int maxHealth = 30;
    public int currentHealth = 30;

    [Header("AP (Action Points)")]
    public int maxAP = 6;
    public int currentAP = 1;

    [Header("Combat Stats")]
    public int attack = 10;
    public int defense = 4;
    public int speed = 10;

    public int stamina = 5;
    public int dmg = 5;
}