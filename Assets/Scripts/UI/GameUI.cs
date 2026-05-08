using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI hudText;

    [Header("Game References")]
    public PlayerHealth playerHealth;
    public DifficultyManager difficultyManager;
    public SectionGenerator sectionGenerator;

    private void Update(){ UpdateHUD(); }

    private void UpdateHUD()
    {
        if (hudText == null) { return; }

        string healthText = GetHealthText();
        string sectionText = GetSectionText();
        string difficultyText = GetDifficultyText();
        string enemiesText = GetEnemiesText();
        string modeText = GetModeText();
        string flowText = GetFlowText();

        hudText.text = healthText + "\n" + sectionText + "\n" + difficultyText + "\n" + enemiesText + "\n" + modeText + "\n" + flowText;
    }

    private string GetHealthText(){ if (playerHealth == null) return "Health: N/A"; return "Health: " + playerHealth.GetCurrentHealth().ToString("F0") + " / " + playerHealth.maxHealth.ToString("F0"); }
    private string GetSectionText(){ SectionInstance currentSection = GetCurrentSection(); if (currentSection == null) return "Section: N/A"; return "Section: " + currentSection.sectionIndex; }
    private string GetDifficultyText(){ if (difficultyManager == null) return "Difficulty: N/A"; return "Difficulty: " + difficultyManager.currentDifficultyScore + " / " + difficultyManager.maxDifficultyScore; }
    private string GetEnemiesText(){ SectionInstance currentSection = GetCurrentSection(); if (currentSection == null) return "Enemies: N/A"; return "Enemies: " + currentSection.enemiesAlive + " / " + currentSection.totalEnemies; }
    private string GetModeText(){ if (difficultyManager == null) return "Mode: N/A"; return difficultyManager.adaptiveDifficultyEnabled ? "Mode: Adaptive" : "Mode: Fixed"; }

    private string GetFlowText()
    {
        if (difficultyManager == null) { return "Flow: N/A"; }

        DifficultyAnalysisResult result = difficultyManager.GetLastAnalysisResult();
        if (result == null) { return "Flow: N/A"; }

        return "Flow: " + result.flowResult + " (" + result.flowScore + ")";
    }

    private SectionInstance GetCurrentSection(){ if (sectionGenerator == null) return null; return sectionGenerator.GetCurrentActiveSection(); }
}
