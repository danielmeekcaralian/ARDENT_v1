using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject infoPanel;
    public GameObject helpPanel;

    public static bool HasOpenPanel => Instance != null &&
        (IsOpen(Instance.menuPanel) || IsOpen(Instance.settingsPanel) || IsOpen(Instance.infoPanel) || IsOpen(Instance.helpPanel));
    private static bool IsOpen(GameObject panel) => panel != null && panel.activeInHierarchy;

    void Awake()
    {
        Instance = this;
        UseExistingEventSystem();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() { CloseButton(); }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { UseExistingEventSystem(); }
    private void UseExistingEventSystem()
    {
        var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        bool another = false;
        foreach (var system in systems)
            if (system.gameObject.scene != gameObject.scene && system.isActiveAndEnabled) another = true;
        foreach (var system in systems)
            if (another && system.gameObject.scene == gameObject.scene) system.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // -------------------
    // PANEL CONTROL
    // -------------------

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    public void OpenInfo()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(true);
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    public void OpenHelp()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(true);
    }

    public void CloseButton()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);

        if (menuPanel != null) menuPanel.SetActive(false);
    }

    // -------------------
    // BUTTON ACTIONS
    // -------------------

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}