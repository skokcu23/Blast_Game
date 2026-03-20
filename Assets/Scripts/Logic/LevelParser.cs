using UnityEngine;

/// <summary>
/// Loads and parses level JSON files into LevelData objects.
///
/// Level files are stored at: Assets/Resources/Levels/level_01.json through level_10.json
/// Unity's Resources.Load strips the .json extension automatically.
/// </summary>
public static class LevelParser
{
    /// <summary>
    /// Load a level by number from the Resources folder.
    /// Returns null if the file is missing or fails validation.
    /// </summary>
    public static LevelData LoadLevel(int levelNumber)
    {
        string path = $"Levels/level_{levelNumber:D2}";
        TextAsset file = Resources.Load<TextAsset>(path);

        if (file == null)
        {
            Debug.LogError($"[LevelParser] Level file not found at Resources/{path}");
            return null;
        }

        return Parse(file.text);
    }

    /// <summary>
    /// Parse a raw JSON string into LevelData.
    /// Validates the result before returning.
    /// </summary>
    public static LevelData Parse(string json)
    {
        LevelData data = JsonUtility.FromJson<LevelData>(json);

        if (data == null)
        {
            Debug.LogError("[LevelParser] Failed to deserialize level JSON.");
            return null;
        }

        if (!data.IsValid())
        {
            Debug.LogError(
                $"[LevelParser] Level {data.level_number} failed validation. " +
                $"Grid: {data.grid_width}x{data.grid_height}, " +
                $"Items: {data.grid?.Length ?? 0} (expected {data.grid_width * data.grid_height})");
            return null;
        }

        return data;
    }

    /// <summary>
    /// Count total available levels by checking sequential files starting from level_01.
    /// Stops at the first missing file number.
    /// </summary>
    public static int GetTotalLevelCount()
    {
        int count = 0;
        while (true)
        {
            string path = $"Levels/level_{(count + 1):D2}";
            if (Resources.Load<TextAsset>(path) == null)
                break;
            count++;
        }
        return count;
    }
}
