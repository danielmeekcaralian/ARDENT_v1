using UnityEngine;
using UnityEngine.SceneManagement;

public class Island : MonoBehaviour
{
    [Header("Island Information")]
    public int islandIndex;

    [Tooltip("Enable this for COCs that are not available yet.")]
    public bool comingSoon;


    [Header("Island Visual Groups")]
    public GameObject unlockedGroup;
    public GameObject lockedGroup;
    public GameObject comingSoonGroup;


    [Header("Island Collider")]
    public Collider islandCollider;


    void Start()
    {
        UpdateState();
    }


    public void UpdateState()
    {
        int unlocked = ProgressManager.Instance.GetUnlockedIsland();

        // -------------------------------------------------
        // COMING SOON
        // -------------------------------------------------

        if (comingSoon)
        {
            if (unlockedGroup != null)
                unlockedGroup.SetActive(false);

            if (lockedGroup != null)
                lockedGroup.SetActive(false);

            if (comingSoonGroup != null)
                comingSoonGroup.SetActive(true);

            // Coming Soon islands cannot be selected
            if (islandCollider != null)
                islandCollider.enabled = false;

            return;
        }


        // -------------------------------------------------
        // NORMAL LOCKED / UNLOCKED
        // -------------------------------------------------

        bool isUnlocked = islandIndex <= unlocked;


        if (unlockedGroup != null)
            unlockedGroup.SetActive(isUnlocked);

        if (lockedGroup != null)
            lockedGroup.SetActive(!isUnlocked);

        if (comingSoonGroup != null)
            comingSoonGroup.SetActive(false);


        // Only unlocked islands can be selected
        if (islandCollider != null)
            islandCollider.enabled = isUnlocked;
    }


    public void SelectIsland()
    {
        Debug.Log("SELECT ISLAND CALLED");


        // Coming Soon islands cannot be selected
        if (comingSoon)
        {
            Debug.Log($"COC {islandIndex + 1} is coming soon.");
            return;
        }


        int unlocked = ProgressManager.Instance.GetUnlockedIsland();


        if (islandIndex <= unlocked)
        {
            string cocID =
                "COC_" + (islandIndex + 1).ToString("00");


            Debug.Log($"Selected COC: {cocID}");


            LessonSession.SetCOC(cocID);

            SceneManager.LoadScene("COCScene");
        }
        else
        {
            Debug.Log($"Island {islandIndex} is locked");
        }
    }
}
