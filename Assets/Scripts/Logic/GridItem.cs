public class GridItem
{
    public Gem GemType;
    public bool IsObstacle;
    public bool IsMovable;
    public int Health;

    // Constructor for easy creation
    public GridItem(Gem type, bool isObstacle = false, bool isMovable = true, int health = 1)
    {
        GemType = type;
        IsObstacle = isObstacle;
        IsMovable = isMovable;
        Health = health;
    }

    // A handy helper property
    public bool IsEmpty => GemType == Gem.NONE;
}
