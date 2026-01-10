using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [Header("Player Status Text")]
    public TextMeshProUGUI currentHPText;
    public TextMeshProUGUI maxHPText;
    public TextMeshProUGUI currentAPText;

    [Header("Command Panel")]
    public GameObject commandPanel;

    
    public CanvasGroup weaponSkillsLabelGroup;

    // Still figuring out how to layer the UI potential future use /MN 
    public CanvasGroup itemsLabelGroup;

    [Header("Command Labels")]
    public GameObject attackLabel;
    public GameObject weaponSkillsLabel;
    public GameObject itemsLabel;
    public GameObject lupusIraLabel;   

    //Status and UI setup /MN 
    public void UpdateHealth(int current, int max)
    {
        UpdateHP(current, max);
    }

    public void UpdateHP(int current, int max)
    {
        if (currentHPText != null)
            currentHPText.text = current.ToString();

        if (maxHPText != null)
            maxHPText.text = max.ToString();
    }

    public void UpdateAP(int current, int max)
    {
        if (currentAPText != null)
            currentAPText.text = current.ToString();
    }

    //COMMAND PANEL /MN
    public void ShowCommandPanel(bool show)
    {
        if (commandPanel != null)
            commandPanel.SetActive(show);
    }

    
    public void SetCommandInteractable(CanvasGroup group, bool isEnabled)
    {
        if (group == null)
            return;

        group.alpha = isEnabled ? 1f : 0.35f;
        group.interactable = isEnabled;
        group.blocksRaycasts = isEnabled;
    }

    public void HighlightCommand(string _ignored) { }
}