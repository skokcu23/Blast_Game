/// <summary>
/// Represents a single cell's contents on the Board.
/// Created exclusively by ItemFactory — never construct with raw values in game code.
/// </summary>
public class GridItem
{
    public string Id { get; private set; }
    public bool IsObstacle { get; private set; }
    public bool IsMovable { get; private set; }
    public int Health { get; set; } // Mutable: obstacles take damage

    public GridItem(string id, bool isObstacle, bool isMovable, int health)
    {
        Id = id;
        IsObstacle = isObstacle;
        IsMovable = isMovable;
        Health = health;
    }

    // --- Derived helpers (no magic strings outside ItemIds) ---

    public bool IsEmpty => Id == ItemIds.None;
    public bool IsCube => ItemIds.IsCube(Id);
    public bool IsRocket => ItemIds.IsRocket(Id);
    public bool IsAlive => Health > 0;

    /// <summary>
    /// Apply one point of damage. Returns true if this killed the item.
    /// </summary>
    public bool TakeDamage(int amount = 1)
    {
        if (IsEmpty) return false;

        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            return true; // Destroyed
        }
        return false;
    }

    /// <summary>
    /// Create a deep copy. Use when you need to place the same data
    /// in a new grid slot without sharing a reference.
    /// </summary>
    public GridItem Clone() => new GridItem(Id, IsObstacle, IsMovable, Health);

    public override string ToString() => $"[{Id} hp:{Health} obs:{IsObstacle} mov:{IsMovable}]";
}
