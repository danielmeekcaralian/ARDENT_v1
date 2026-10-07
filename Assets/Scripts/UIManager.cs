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
        if (menuPanel != null)
            foreach (var button in menuPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (button.name == "mainMenuButton")
                {
                    var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (label != null) label.text = ARSandboxSession.IsActive ? "Return to Library" : "Main Menu";
                }
        ArdentMotion.SetPanelVisible(menuPanel, true);
        ArdentMotion.SetPanelVisible(settingsPanel, false);
        ArdentMotion.SetPanelVisible(infoPanel, false);
        ArdentMotion.SetPanelVisible(helpPanel, false);
    }

    public void OpenSettings()
    {
        ArdentMotion.SetPanelVisible(menuPanel, false);
        ArdentMotion.SetPanelVisible(settingsPanel, true);
        ArdentMotion.SetPanelVisible(infoPanel, false);
        ArdentMotion.SetPanelVisible(helpPanel, false);
    }

    public void OpenInfo()
    {
        ArdentMotion.SetPanelVisible(menuPanel, false);
        ArdentMotion.SetPanelVisible(settingsPanel, false);
        ArdentMotion.SetPanelVisible(infoPanel, true);
        ArdentMotion.SetPanelVisible(helpPanel, false);
    }

    public void OpenHelp()
    {
        ArdentMotion.SetPanelVisible(menuPanel, false);
        ArdentMotion.SetPanelVisible(settingsPanel, false);
        ArdentMotion.SetPanelVisible(infoPanel, false);
        ArdentMotion.SetPanelVisible(helpPanel, true);
    }

    public void CloseButton()
    {
        ArdentMotion.SetPanelVisible(settingsPanel, false);
        ArdentMotion.SetPanelVisible(infoPanel, false);
        ArdentMotion.SetPanelVisible(helpPanel, false);
        ArdentMotion.SetPanelVisible(menuPanel, false);
    }

    // -------------------
    // BUTTON ACTIONS
    // -------------------

    public void GoToMainMenu()
    {
        ARCheckpointSession.SaveCurrent();
        ArdentMotion.LoadScene(ARSandboxSession.IsActive ? "Hardware_Library" : "MainMenu");
    }

    public void QuitGame()
    {
        ARCheckpointSession.SaveCurrent();
        Application.Quit();
        Debug.Log("Game Quit");
    }
}
