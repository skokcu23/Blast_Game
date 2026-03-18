/// <summary>
/// Abstraction for saving/loading level progress.
/// The logic layer uses this interface; the View layer provides a PlayerPrefs implementation.
/// This keeps the logic layer pure C# and fully testable.
/// </summary>
public interface ILevelPersistence
{
    /// <summary>Load the last saved level number. Returns 1 if no save exists.</summary>
    int LoadCurrentLevel();

    /// <summary>Save the current level number.</summary>
    void SaveCurrentLevel(int levelNumber);
}
