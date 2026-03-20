/// <summary>
/// Represents a single cell's contents on the Board.
/// Created exclusively by ItemFactory — never construct directly in game code.
///
/// Health is mutable (obstacles take damage). All other properties are read-only after creation.
/// </summary>
public class GridItem
{
    /// <summary>Item type identifier (matches ItemIds constants).</summary>
    public string Id { get; private set; }

    /// <summary>True for Box, Stone, Vase. These are level goals.</summary>
    public bool IsObstacle { get; private set; }

    /// <summary>True if this item falls during gravity. False for Box and Stone (fixed in place).</summary>
    public bool IsMovable { get; private set; }

    /// <summary>Current health. Decremented by DamageResolver. 0 = destroyed.</summary>
    public int Health { get; set; }

    public GridItem(string id, bool isObstacle, bool isMovable, int health)
    {
        Id = id;
        IsObstacle = isObstacle;
        IsMovable = isMovable;
        Health = health;
    }

    // --- Derived properties ---

    /// <summary>True if this cell has no item.</summary>
    public bool IsEmpty => Id == ItemIds.None;

    /// <summary>True if this is a color cube (r, g, b, y).</summary>
    public bool IsCube => ItemIds.IsCube(Id);

    /// <summary>True if this is a rocket (vro, hro).</summary>
    public bool IsRocket => ItemIds.IsRocket(Id);

    /// <summary>True if health is above zero.</summary>
    public bool IsAlive => Health > 0;

    /// <summary>
    /// Apply damage. Returns true if this killed the item (health reached 0).
    /// </summary>
    public bool TakeDamage(int amount = 1)
    {
        if (IsEmpty) return false;

        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            return true;
        }
        return false;
    }

    /// <summary>Create a deep copy (separate reference, same data).</summary>
    public GridItem Clone() => new GridItem(Id, IsObstacle, IsMovable, Health);

    public override string ToString() => $"[{Id} hp:{Health} obs:{IsObstacle} mov:{IsMovable}]";
}
