using System;

/// <summary>
/// Immutable 2D grid coordinate used throughout the game.
///
/// Implements value equality so it works correctly as a Dictionary key
/// (critical for GridStateManager's cell registry and Board's item lookup).
///
/// Convention: (0,0) is bottom-left of the grid. X increases right, Y increases up.
/// </summary>
public struct Coordinate : IEquatable<Coordinate>
{
    /// <summary>Horizontal position (0 = leftmost column).</summary>
    public readonly int x;

    /// <summary>Vertical position (0 = bottom row).</summary>
    public readonly int y;

    public Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(Coordinate other) => x == other.x && y == other.y;
    public override bool Equals(object obj) => obj is Coordinate other && Equals(other);
    public override int GetHashCode() => x * 397 ^ y;

    public static bool operator ==(Coordinate a, Coordinate b) => a.Equals(b);
    public static bool operator !=(Coordinate a, Coordinate b) => !a.Equals(b);

    public override string ToString() => $"({x}, {y})";
}
