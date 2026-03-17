using UnityEngine;

/// <summary>
/// Reads level JSON files and produces LevelData objects.
///
/// Level files live in: Resources/Levels/level_01.json, level_02.json, etc.
/// (Unity strips the .json extension when loading via Resources.Load)
/// </summary>
public static class LevelParser
{
    /// <summary>
    /// Load a level from Unity's Resources folder.
    /// File must be at: Resources/Levels/level_{number:D2}
    /// </summary>
    public static LevelData LoadLevel(int levelNumber)
    {
        string path = $"Levels/level_{levelNumber:D2}";
        TextAsset file = Resources.Load<TextAsset>(path);

        if (file == null)
        {
            Debug.LogError(
                $"[LevelParser] Could not find level file at Resources/{path}. "
                    + $"Make sure the file exists and is named correctly."
            );
            return null;
        }

        return Parse(file.text);
    }

    /// <summary>
    /// Parse a raw JSON string into LevelData.
    /// This overload is testable without Unity (pass any JSON string).
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
                $"[LevelParser] Level {data.level_number} failed validation. "
                    + $"Grid: {data.grid_width}x{data.grid_height}, "
                    + $"Items: {data.grid?.Length ?? 0} (expected {data.grid_width * data.grid_height})"
            );
            return null;
        }

        return data;
    }

    /// <summary>
    /// Check how many level files exist in Resources/Levels/.
    /// Counts sequentially from level_01 until a file is not found.
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
