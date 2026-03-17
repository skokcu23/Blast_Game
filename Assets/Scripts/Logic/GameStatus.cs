// GameStatus.cs - DTO to send results from Logic to Orchestrator
using System.Collections.Generic;

public class BlastResult
{
    public bool IsValid;
    public List<Coordinate> BlastedCoordinates;
    public int RemainingMoves;
}
