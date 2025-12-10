using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PartyData", menuName = "Game Data/Party")]
public class PartyData : ScriptableObject
{
    public List<CharacterData> currentParty = new List<CharacterData>();
}