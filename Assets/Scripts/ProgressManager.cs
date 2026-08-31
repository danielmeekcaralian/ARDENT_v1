using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public int GetUnlockedIsland()
    {
        return PlayerPrefs.GetInt("UnlockedIsland", 0);
    }

    public void UnlockNextIsland(int completedIslandIndex)
    {
        int unlocked = GetUnlockedIsland();

        if (completedIslandIndex >= unlocked)
        {
            int nextIsland = completedIslandIndex + 1;

            PlayerPrefs.SetInt("UnlockedIsland", nextIsland);
            PlayerPrefs.Save();
        }
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("UnlockedIsland");
    }
}