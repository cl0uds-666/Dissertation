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

        SceneManager.LoadScene(gameplaySceneName);
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

    public void ClearCsvInEditor()
    {
#if UNITY_EDITOR
        CSVLogger logger = FindAnyObjectByType<CSVLogger>();
        if (logger == null)
        {
            Debug.LogWarning("MenuController: No CSVLogger found in active scene.");
            return;
        }

        logger.ClearCsvLog();
#else
        Debug.LogWarning("ClearCsvInEditor is editor-only.");
#endif
    }

    public void OpenCsvFolderInEditor()
    {
#if UNITY_EDITOR
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        UnityEditor.EditorUtility.RevealInFinder(path);
#else
        Debug.LogWarning("OpenCsvFolderInEditor is editor-only.");
#endif
    }

    private void SetPanelState(bool showMain, bool showAbout, bool showControls)
    {
        if (mainPanel != null) mainPanel.SetActive(showMain);
        if (aboutPanel != null) aboutPanel.SetActive(showAbout);
        if (controlsPanel != null) controlsPanel.SetActive(showControls);
    }
}
