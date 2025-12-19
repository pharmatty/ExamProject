using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("Command Labels")]
    public TextMeshProUGUI attackLabel;
    public TextMeshProUGUI skillsLabel;
    public TextMeshProUGUI itemsLabel;
    public TextMeshProUGUI runLabel;

    [Header("HP Display")]
    public TextMeshProUGUI currentHPText;
    public TextMeshProUGUI maxHPText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    public void HighlightCommand(string command)
    {
        attackLabel.color = normalColor;
        skillsLabel.color = normalColor;
        itemsLabel.color = normalColor;
        runLabel.color = normalColor;

        if (command == "Attack") attackLabel.color = highlightColor;
        if (command == "Skills") skillsLabel.color = highlightColor;
        if (command == "Items") itemsLabel.color = highlightColor;
        if (command == "Run") runLabel.color = highlightColor;
    }

    public void ShowCommandPanel(bool show)
    {
        gameObject.SetActive(show);
    }

    public void UpdateHealth(int current, int max)
    {
        currentHPText.text = current.ToString();
        maxHPText.text = max.ToString();
    }
}