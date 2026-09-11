using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    private bool uiReady => UIManager.Instance != null;

    void Start()
    {
        if (UIManager.Instance == null)
        {
            SceneManager.LoadScene(
                "UI_Scene",
                LoadSceneMode.Additive
            );
        }
    }

    public void OpenSettings()
    {
        if (!uiReady) return;

        UIManager.Instance.OpenSettings();
    }

    public void OpenInfo()
    {
        if (!uiReady) return;

        UIManager.Instance.OpenInfo();
    }

    public void OpenHelp()
    {
        if (!uiReady) return;

        UIManager.Instance.OpenHelp();
    }

    public void OpenMenu()
    {
        if (!uiReady) return;

        UIManager.Instance.ShowMenu();
    }
}