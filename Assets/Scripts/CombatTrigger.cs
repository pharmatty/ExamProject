using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTrigger : MonoBehaviour
{
    public string combatSceneName = "CombatScene";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        // 🔒 SAVE RETURN POINT BEFORE COMBAT
        GameManager.Instance.SaveBattleReturnPoint(other.transform);

        SceneManager.LoadScene(combatSceneName);
    }
}