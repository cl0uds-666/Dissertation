using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string gameplaySceneName = "Main";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject controlsPanel;

    private void Start()
    {
        ShowMainPanel();
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("MenuController: Gameplay scene name is empty.");
            return;
        }

        GameModeSelection.StartNewSession();
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void SelectAdaptiveMode() { GameModeSelection.SetMode(DifficultyExperimentMode.Adaptive); }
    public void SelectFixedEasyMode() { GameModeSelection.SetMode(DifficultyExperimentMode.FixedEasy); }
    public void SelectFixedMediumMode() { GameModeSelection.SetMode(DifficultyExperimentMode.FixedMedium); }
    public void SelectFixedHardMode() { GameModeSelection.SetMode(DifficultyExperimentMode.FixedHard); }

    public void OpenAbout()
    {
        SetPanelState(showMain: false, showAbout: true, showControls: false);
    }

    public void OpenControls()
    {
        SetPanelState(showMain: false, showAbout: false, showControls: true);
    }

    public void ShowMainPanel()
    {
        SetPanelState(showMain: true, showAbout: false, showControls: false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPanelState(bool showMain, bool showAbout, bool showControls)
    {
        if (mainPanel != null) mainPanel.SetActive(showMain);
        if (aboutPanel != null) aboutPanel.SetActive(showAbout);
        if (controlsPanel != null) controlsPanel.SetActive(showControls);
    }
}
