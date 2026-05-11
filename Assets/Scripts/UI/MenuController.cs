using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string gameplaySceneName = "Main";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject controlsPanel;
    [Header("Mode Selection")]
    [SerializeField] private TMP_Dropdown difficultyModeDropdown;

    private void Start()
    {
        ShowMainPanel();
        SetupDifficultyDropdown();
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

    public void OnDifficultyDropdownChanged(int optionIndex)
    {
        switch (optionIndex)
        {
            case 0: SelectAdaptiveMode(); break;
            case 1: SelectFixedEasyMode(); break;
            case 2: SelectFixedMediumMode(); break;
            case 3: SelectFixedHardMode(); break;
            default: SelectAdaptiveMode(); break;
        }
    }

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

    private void SetupDifficultyDropdown()
    {
        if (difficultyModeDropdown == null)
        {
            SelectAdaptiveMode();
            return;
        }

        difficultyModeDropdown.onValueChanged.RemoveListener(OnDifficultyDropdownChanged);
        difficultyModeDropdown.ClearOptions();
        difficultyModeDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "Adaptive",
            "Fixed Easy",
            "Fixed Medium",
            "Fixed Hard"
        });
        difficultyModeDropdown.value = 0;
        difficultyModeDropdown.RefreshShownValue();
        difficultyModeDropdown.onValueChanged.AddListener(OnDifficultyDropdownChanged);
        OnDifficultyDropdownChanged(difficultyModeDropdown.value);
    }
}
