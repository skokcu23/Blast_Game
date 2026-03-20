using System;

/// <summary>
/// Data Transfer Object for level JSON files.
///
/// Matches the JSON structure defined in the Case Study:
///   { level_number, grid_width, grid_height, move_count, grid[] }
///
/// The grid array is stored bottom-left → top-right, row by row.
/// Index formula: grid[y * grid_width + x]
///
/// Deserialized by LevelParser via JsonUtility.
/// </summary>
[Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public string[] grid;

    /// <summary>
    /// Get the item ID at a specific grid coordinate.
    /// Returns ItemIds.None if the coordinate is out of bounds.
    /// </summary>
    public string GetItemIdAt(int x, int y)
    {
        int index = y * grid_width + x;
        if (index < 0 || index >= grid.Length)
            return ItemIds.None;
        return grid[index];
    }

    /// <summary>
    /// Validate against Case Study constraints:
    /// grid dimensions 6–10, positive move count, correct grid array length.
    /// </summary>
    public bool IsValid()
    {
        if (grid_width < 6 || grid_width > 10) return false;
        if (grid_height < 6 || grid_height > 10) return false;
        if (move_count <= 0) return false;
        if (grid == null || grid.Length != grid_width * grid_height) return false;
        return true;
    }
}
