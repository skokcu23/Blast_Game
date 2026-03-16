using System;

public class Board
{
    private Gem[,] grid;

    private int width;
    public int Width
    {
        get { return width; }
        set { width = value; }
    }

    private int height;
    public int Height
    {
        get { return height; }
        set { height = value; }
    }

    private Random rand;

    public Board(int width, int height)
    {
        grid = new Gem[width, height];
        rand = new Random();
    }

    public void Initialize()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = GenerateRandomGem();
            }
        }
    }

    //Getter and Setters
    public Gem GetGem(Coordinate coordinate)
    {
        return grid[coordinate.x, coordinate.y];
    }

    public Gem GetGem(int x, int y)
    {
        return grid[x, y];
    }

    public void SetGem(Coordinate coordinate, Gem gem)
    {
        grid[coordinate.x, coordinate.y] = gem;
    }

    //Utility
    private Gem GenerateRandomGem()
    {
        int randomInt = rand.Next(0, 4);

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
