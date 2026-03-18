using UnityEngine;

/// <summary>
/// Unity implementation of ILevelPersistence using PlayerPrefs.
/// Persists the current level number across game restarts.
///
/// Case Study: "The user's last played level number should be persisted locally."
/// </summary>
public class PlayerPrefsLevelPersistence : ILevelPersistence
{
    private const string LevelKey = "CurrentLevel";

    public int LoadCurrentLevel()
    {
        return PlayerPrefs.GetInt(LevelKey, 1); // Default to level 1
    }

    public void SaveCurrentLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(LevelKey, levelNumber);
        PlayerPrefs.Save();
    }
}
