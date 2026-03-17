// GemMovement.cs
// A simple data packet that says "A gem of this type moved from point A to point B"
public struct GemMovement
{
    public Coordinate StartPos;
    public Coordinate EndPos;
    public Gem GemType;
}
