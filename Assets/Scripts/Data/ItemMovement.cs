/// <summary>
/// DTO: Describes one item moving from point A to point B.
/// Used by GravityProcessor to communicate movements to BoardView.
/// Renamed from GemMovement — items are no longer just "gems".
/// </summary>
public struct ItemMovement
{
    public Coordinate StartPos;
    public Coordinate EndPos;
    public string ItemId;
}
