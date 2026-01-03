using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [Header("Player Status Text")]
    public TextMeshProUGUI currentHPText;
    public TextMeshProUGUI maxHPText;
    public TextMeshProUGUI currentSPText;

    [Header("Command Panel")]
    public GameObject commandPanel;

    // CanvasGroup on SkillsLabel
    public CanvasGroup skillsLabelGroup;

    // (optional future)
    public CanvasGroup itemsLabelGroup;

    [Header("Command Labels")]
    public GameObject attackLabel;
    public GameObject skillsLabel;
    public GameObject itemsLabel;
    public GameObject runLabel;

    // =========================
    // STATUS UI (BACK-COMPAT)
    // =========================

    // Existing API used by CombatManager / BattleUnit
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

    public void UpdateSP(int current, int max)
    {
        // we only show current SP in UI, but
        // we keep the signature for compatibility
        if (currentSPText != null)
            currentSPText.text = current.ToString();
    }

    // =========================
    // COMMAND PANEL
    // =========================
    public void ShowCommandPanel(bool show)
    {
        if (commandPanel != null)
            commandPanel.SetActive(show);
    }

    // =========================
    // FADE-OUT SUPPORT
    // =========================
    public void SetCommandInteractable(CanvasGroup group, bool isEnabled)
    {
        if (group == null)
            return;

        group.alpha = isEnabled ? 1f : 0.35f;
        group.interactable = isEnabled;
        group.blocksRaycasts = isEnabled;
    }

    // No highlight logic — input-driven
    public void HighlightCommand(string _ignored) {}
}
