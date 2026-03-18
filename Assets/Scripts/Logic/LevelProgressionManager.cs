/// <summary>
/// Pure C# manager for level progression.
///
/// Responsibilities:
///   - Track current level number
///   - Know total number of levels
///   - Advance after winning a level
///   - Detect "all levels finished" state
///   - Persist progress via ILevelPersistence
///
/// Case Study requirements:
///   - LevelButton displays current level number
///   - When all levels finished, display "Finished"
///   - Level number persisted across Unity restarts
///   - Editor menu item can set level number
/// </summary>
public class LevelProgressionManager
{
    private readonly ILevelPersistence _persistence;
    private readonly int _totalLevels;
    private int _currentLevel;

    /// <summary>Current level (1-based). May be > TotalLevels if all complete.</summary>
    public int CurrentLevel => _currentLevel;

    /// <summary>Total number of levels available.</summary>
    public int TotalLevels => _totalLevels;

    /// <summary>True when all levels have been cleared.</summary>
    public bool AllLevelsComplete => _currentLevel > _totalLevels;

    /// <summary>
    /// Create with persistence and total level count.
    /// Loads the current level from persistence automatically.
    /// </summary>
    public LevelProgressionManager(ILevelPersistence persistence, int totalLevels)
    {
        _persistence = persistence;
        _totalLevels = totalLevels;
        _currentLevel = _persistence.LoadCurrentLevel();

        // Clamp to valid range (1 to totalLevels+1)
        // totalLevels+1 means "all complete"
        if (_currentLevel < 1) _currentLevel = 1;
        if (_currentLevel > _totalLevels + 1) _currentLevel = _totalLevels + 1;
    }

    /// <summary>
    /// Create without persistence (for testing). Starts at level 1.
    /// </summary>
    public LevelProgressionManager(int totalLevels)
    {
        _persistence = null;
        _totalLevels = totalLevels;
        _currentLevel = 1;
    }

    /// <summary>
    /// Advance to the next level after a win. Persists the new level number.
    /// No-op if all levels are already complete.
    /// </summary>
    public void AdvanceLevel()
    {
        if (AllLevelsComplete) return;

        _currentLevel++;
        _persistence?.SaveCurrentLevel(_currentLevel);
    }

    /// <summary>
    /// Force-set the current level (used by Unity Editor menu item).
    /// Clamps to valid range.
    /// </summary>
    public void SetLevel(int levelNumber)
    {
        if (levelNumber < 1) levelNumber = 1;
        if (levelNumber > _totalLevels + 1) levelNumber = _totalLevels + 1;

        _currentLevel = levelNumber;
        _persistence?.SaveCurrentLevel(_currentLevel);
    }

    /// <summary>
    /// Get the level number to load for gameplay.
    /// Returns the current level, or -1 if all levels are complete.
    /// </summary>
    public int GetLevelToPlay()
    {
        if (AllLevelsComplete) return -1;
        return _currentLevel;
    }

    /// <summary>
    /// Get the display text for the level button.
    /// Returns "Level X" or "Finished".
    /// </summary>
    public string GetLevelButtonText()
    {
        if (AllLevelsComplete) return "Finished";
        return $"Level {_currentLevel}";
    }
}
