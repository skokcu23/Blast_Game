using System;

public class Board
{
    private Gem[,] _grid;

    private int _width;
    public int Width
    {
        get { return _width; }
        set { _width = value; }
    }

    private int _height;
    public int Height
    {
        get { return _height; }
        set { _height = value; }
    }

    private Random _rand;

    public Board(int width, int height)
    {
        _grid = new Gem[_width, _height];
        _rand = new System.Random();
    }

    public void Initialize()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _grid[x, y] = GenerateRandomGem();
            }
        }
    }

    //Getter and Setters
    public Gem GetGem(Coordinate coordinate)
    {
        return _grid[coordinate.x, coordinate.y];
    }

    public Gem GetGem(int x, int y)
    {
        return _grid[x, y];
    }

    public void SetGem(Coordinate coordinate, Gem gem)
    {
        _grid[coordinate.x, coordinate.y] = gem;
    }

    //Utility
    private Gem GenerateRandomGem()
    {
        int randomInt = _rand.Next(0, 5);

        switch (randomInt)
        {
            case 0:
                return Gem.NONE;
            case 1:
                return Gem.BLUE;
            case 2:
                return Gem.YELLOW;
            case 3:
                return Gem.GREEN;
            case 4:
                return Gem.RED;
            default:
                return Gem.NONE;
        }
    }
}
