using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start Learning button
    public void LoadStartLearning()
    {
        SceneManager.LoadScene("Start_Learning");
    }

    // Hardware Library button
    public void LoadHardwareLibrary()
    {
        SceneManager.LoadScene("Hardware_Library");
    }

    // Back button
    public void GoBack()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        switch (currentScene)
        {
            case "Start_Learning":
                SceneManager.LoadScene("MainMenu");
                break;

            case "COCScene":
                SceneManager.LoadScene("Start_Learning");
                break;

            case "ActivitySelectionScene":
                SceneManager.LoadScene("COCScene");
                break;

            case "LessonScene":
                SceneManager.LoadScene("ActivitySelectionScene");
                break;

            case "ARScene":
                SceneManager.LoadScene("ActivitySelectionScene");
                break;

            case "QuizScene":
                SceneManager.LoadScene("ActivitySelectionScene");
                break;

            case "Hardware_Library":
                SceneManager.LoadScene("MainMenu");
                break;

            default:
                Debug.LogWarning(
                    "No Back destination defined for scene: " +
                    currentScene
                );
                break;
        }
    }
}