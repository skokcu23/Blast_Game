/// <summary>
/// Describes one item moving from one grid position to another.
/// Used by GravityProcessor (falling) and refill (new items entering from top).
///
/// For gravity: StartPos is where the item was, EndPos is where it lands.
/// For refill:  StartPos is above the board (spawn row), EndPos is the target cell.
///              ItemId identifies what was spawned.
/// </summary>
public struct ItemMovement
{
    /// <summary>Grid position the item moves from.</summary>
    public Coordinate StartPos;

    /// <summary>Grid position the item moves to.</summary>
    public Coordinate EndPos;

    /// <summary>Item type identifier (used by refill to spawn the correct visual).</summary>
    public string ItemId;
}
