using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    //Start Learning button
    public void LoadStartLearning()
    {
        SceneManager.LoadScene("Start_Learning");
    }

    //Hardware Library button
    public void LoadHardwareLibrary()
    {
        SceneManager.LoadScene("Hardware_Library");
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}