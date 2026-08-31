using UnityEngine;
using UnityEngine.SceneManagement;

public class Island : MonoBehaviour
{
    public int islandIndex;

    [Header("Island Visual Groups")]
    public GameObject unlockedGroup;
    public GameObject lockedGroup;

    [Header("Island Collider")]
    public Collider islandCollider;


    void Start()
    {
        UpdateState();
    }


    public void UpdateState()
    {
        int unlocked = ProgressManager.Instance.GetUnlockedIsland();

        bool isUnlocked = islandIndex <= unlocked;

        // Change visuals
        unlockedGroup.SetActive(isUnlocked);
        lockedGroup.SetActive(!isUnlocked);

        // Enable/disable clicking
        islandCollider.enabled = isUnlocked;
    }


    public void SelectIsland()
    {
        int unlocked = ProgressManager.Instance.GetUnlockedIsland();

        if (islandIndex <= unlocked)
        {
            string cocSceneName =
                "COC_" + (islandIndex + 1).ToString("00");

            Debug.Log("Loading " + cocSceneName);

            LessonSession.SetCOC(cocSceneName);

            SceneManager.LoadScene(cocSceneName);
        }
        else
        {
            Debug.Log("Island " + islandIndex + " is locked");
        }
    }
}