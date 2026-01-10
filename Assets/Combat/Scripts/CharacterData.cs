using UnityEngine;

[CreateAssetMenu(menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    //Health /MN
    [Header("Health")]
    public int maxHealth = 30;
    public int currentHealth = 30;
    //AP /MN
    [Header("AP (Action Points)")]
    public int maxAP = 6;
    public int currentAP = 1;
    //Combat Stats /MN
    [Header("Combat Stats")]
    public int attack = 10;
    public int defense = 4;
    public int speed = 10;

    public int stamina = 5;
    public int dmg = 5;
}