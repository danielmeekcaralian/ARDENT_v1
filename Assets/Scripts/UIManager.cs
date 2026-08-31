using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject infoPanel;
    public GameObject helpPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
    }

    // -------------------
    // PANEL CONTROL
    // -------------------

    public void ShowMenu()
    {
        menuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        infoPanel.SetActive(false);
        helpPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        infoPanel.SetActive(false);
        helpPanel.SetActive(false);
    }

    public void OpenInfo()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        infoPanel.SetActive(true);
        helpPanel.SetActive(false);
    }

    public void OpenHelp()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        infoPanel.SetActive(false);
        helpPanel.SetActive(true);
    }

    public void CloseButton()
    {
        settingsPanel.SetActive(false);
        infoPanel.SetActive(false);
        helpPanel.SetActive(false);

        menuPanel.SetActive(false);
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