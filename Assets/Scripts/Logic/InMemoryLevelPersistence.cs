/// <summary>
/// In-memory implementation of ILevelPersistence for testing.
/// No Unity dependency — just stores a value in a field.
/// </summary>
public class InMemoryLevelPersistence : ILevelPersistence
{
    private int _savedLevel;

    public InMemoryLevelPersistence(int initialLevel = 1)
    {
        _savedLevel = initialLevel;
    }

    public int LoadCurrentLevel() => _savedLevel;

    public void SaveCurrentLevel(int levelNumber)
    {
        _savedLevel = levelNumber;
    }
}
