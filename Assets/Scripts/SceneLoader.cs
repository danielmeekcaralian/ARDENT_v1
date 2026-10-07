using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start Learning button
    public void LoadStartLearning()
    {
        ArdentMotion.LoadScene("Start_Learning");
    }

    // Hardware Library button
    public void LoadHardwareLibrary()
    {
        ArdentMotion.LoadScene("Hardware_Library");
    }

    // Back button
    public void GoBack()
    {
        ARCheckpointSession.SaveCurrent();
        string currentScene = SceneManager.GetActiveScene().name;

        switch (currentScene)
        {
            case "Start_Learning":
                ArdentMotion.LoadScene("MainMenu");
                break;

            case "COCScene":
                ArdentMotion.LoadScene("Start_Learning");
                break;

            case "ActivitySelectionScene":
                ArdentMotion.LoadScene("COCScene");
                break;

            case "LessonScene":
                ArdentMotion.LoadScene("ActivitySelectionScene");
                break;

            case "ARScene":
                ArdentMotion.LoadScene(ARSandboxSession.IsActive ? "Hardware_Library" : "ActivitySelectionScene");
                break;

            case "QuizScene":
                ArdentMotion.LoadScene("ActivitySelectionScene");
                break;

            case "Hardware_Library":
                ArdentMotion.LoadScene("MainMenu");
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
