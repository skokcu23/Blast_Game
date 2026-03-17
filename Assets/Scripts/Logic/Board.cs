using System;

/// <summary>
/// Pure C# grid that holds the logical state of all cells.
/// No MonoBehaviour, no UnityEngine dependency — fully NUnit-testable.
/// </summary>
public class Board
{
    private readonly GridItem[,] _grid;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Board(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new GridItem[Width, Height];

        // Fill with empty items so no cell is ever null
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _grid[x, y] = ItemFactory.CreateEmpty();
    }

    // --- Initialization ---

    /// <summary>
    /// Populate the board from a parsed level file.
    /// Grid data in LevelData is stored bottom-left → top-right.
    /// </summary>
    public void Initialize(LevelData levelData)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                string itemId = levelData.GetItemIdAt(x, y);
                _grid[x, y] = ItemFactory.CreateItem(itemId);
            }
        }
    }

    /// <summary>
    /// Fill the entire board with random cubes. Useful for testing
    /// and as a fallback if no level data is available.
    /// </summary>
    public void InitializeRandom()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _grid[x, y] = ItemFactory.CreateRandomCube();
    }

    // --- Getters and Setters ---

    public GridItem GetItem(Coordinate coord) => GetItem(coord.x, coord.y);

    public GridItem GetItem(int x, int y)
    {
        if (IsValidCoordinate(x, y))
            return _grid[x, y];

        // Out-of-bounds returns a safe empty item (fail gracefully)
        return ItemFactory.CreateEmpty();
    }

    public void SetItem(Coordinate coord, GridItem item)
    {
        if (IsValidCoordinate(coord.x, coord.y))
            _grid[coord.x, coord.y] = item;
    }

    public void SetItem(int x, int y, GridItem item)
    {
        if (IsValidCoordinate(x, y))
            _grid[x, y] = item;
    }

    // --- Queries ---

    public bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Check whether all obstacles on the board have been destroyed.
    /// This is the win condition for every level.
    /// </summary>
    public bool AreAllObstaclesCleared()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_grid[x, y].IsObstacle && _grid[x, y].IsAlive)
                    return false;
        return true;
    }

    /// <summary>
    /// Count remaining obstacles (for UI goal display).
    /// </summary>
    public int CountObstacles(string obstacleId = null)
    {
        int count = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var item = _grid[x, y];
                if (!item.IsObstacle || !item.IsAlive)
                    continue;
                if (obstacleId == null || item.Id == obstacleId)
                    count++;
            }
        }
        return count;
    }
}
