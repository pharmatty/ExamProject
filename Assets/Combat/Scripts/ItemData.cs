using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Game Data/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;

    public int healAmount = 0;
    public Sprite icon; // optional
}