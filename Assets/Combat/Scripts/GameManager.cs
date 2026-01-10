using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Global Party Data")]
    public PartyData partyData;

    [Header("Player Runtime Stats")]
    public int playerMaxHealth;
    public int playerCurrentHealth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePlayerHealth(int current, int max)
    {
        playerCurrentHealth = current;
        playerMaxHealth = max;
    }

    public void LoadPlayerHealth(CharacterData character)
    {
        if (playerMaxHealth > 0)
        {
            character.maxHealth = playerMaxHealth;
            character.currentHealth = playerCurrentHealth;
        }
    }
}