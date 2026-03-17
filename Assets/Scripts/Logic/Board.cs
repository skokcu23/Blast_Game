using System;

public class Board
{
    // Make these readonly since we don't reassign the array or random instance
    private readonly Gem[,] _grid;
    private readonly Random _rand;

    // Use auto-properties with private setters to protect them from outside changes
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Board(int width, int height)
    {
        // Assign properties first!
        Width = width;
        Height = height;

        // Now the grid will be the correct size
        _grid = new Gem[Width, Height];
        _rand = new Random();
    }

    public void Initialize()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _grid[x, y] = GenerateRandomGem();
            }
        }
    }

    // --- Getters and Setters ---

    // Arrow functions simplify methods that just call other methods
    public Gem GetGem(Coordinate coordinate) => GetGem(coordinate.x, coordinate.y);

    public Gem GetGem(int x, int y)
    {
        if (IsValidCoordinate(x, y))
        {
            return _grid[x, y];
        }

        // Returning Gem.NONE is usually safer for blast games than throwing an Exception
        // when checking out-of-bounds (like checking above the top row).
        return Gem.NONE;
    }

    public void SetGem(Coordinate coordinate, Gem gem)
    {
        if (IsValidCoordinate(coordinate.x, coordinate.y))
        {
            _grid[coordinate.x, coordinate.y] = gem;
        }
    }

    public void SetGem(int x, int y, Gem gem)
    {
        if (IsValidCoordinate(x, y))
        {
            _grid[x, y] = gem;
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
