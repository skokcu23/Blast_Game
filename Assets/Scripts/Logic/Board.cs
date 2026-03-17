using System;

public class Board
{
    // Make these readonly since we don't reassign the array or random instance
    private readonly GridItem[,] _grid; // Changed from Gem[,]
    private readonly Random _rand;

    // Use auto-properties with private setters to protect them from outside changes
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Board(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new GridItem[Width, Height];
        _rand = new Random();
    }

    public void Initialize()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _grid[x, y] = new GridItem(GenerateRandomGem());
            }
        }
    }

    // --- Getters and Setters ---
    public GridItem GetItem(Coordinate coordinate) => GetItem(coordinate.x, coordinate.y);

    public GridItem GetItem(int x, int y)
    {
        if (IsValidCoordinate(x, y))
        {
            return _grid[x, y];
        }

        // Return a safe "Empty" item if checking out of bounds
        return new GridItem(Gem.NONE, false, false, 0);
    }

    public void SetItem(Coordinate coordinate, GridItem item)
    {
        if (IsValidCoordinate(coordinate.x, coordinate.y))
        {
            _grid[coordinate.x, coordinate.y] = item;
        }
    }

    public void SetItem(int x, int y, GridItem item)
    {
        if (IsValidCoordinate(x, y))
        {
            _grid[x, y] = item;
        }
    }

    // --- Utility ---

    // A central method to check boundaries prevents IndexOutOfRangeExceptions
    public bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    private Gem GenerateRandomGem()
    {
        // Modern C# 8+ switch expression: much cleaner than a standard switch statement
        return _rand.Next(0, 5) switch
        {
            1 => Gem.BLUE,
            2 => Gem.YELLOW,
            3 => Gem.GREEN,
            4 => Gem.RED,
            _ => Gem.NONE,
        };
    }
}
