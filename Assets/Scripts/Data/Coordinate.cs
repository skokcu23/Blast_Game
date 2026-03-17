using System;

/// <summary>
/// Immutable grid coordinate. Used as Dictionary key in BoardView,
/// so Equals/GetHashCode MUST be implemented correctly.
/// </summary>
public struct Coordinate : IEquatable<Coordinate>
{
    public readonly int x;
    public readonly int y;

    public Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // --- Equality (critical for Dictionary<Coordinate, CubeView>) ---

    public bool Equals(Coordinate other) => x == other.x && y == other.y;

    public override bool Equals(object obj) => obj is Coordinate other && Equals(other);

    public override int GetHashCode() => x * 397 ^ y;

    public static bool operator ==(Coordinate a, Coordinate b) => a.Equals(b);
    public static bool operator !=(Coordinate a, Coordinate b) => !a.Equals(b);

    public override string ToString() => $"({x}, {y})";
}
