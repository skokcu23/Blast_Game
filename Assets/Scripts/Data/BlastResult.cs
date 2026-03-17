using System.Collections.Generic;

/// <summary>
/// DTO: Result of a blast operation, passed from Logic to Orchestrator.
/// Contains everything the Orchestrator needs to drive animations and check state.
/// </summary>
public class BlastResult
{
    public bool IsValid;
    public List<Coordinate> BlastedCoordinates;
    public List<Coordinate> DamagedObstacles; // Obstacles that took damage but survived
    public List<Coordinate> DestroyedObstacles; // Obstacles that were killed
    public int RemainingMoves;
    public bool ShouldCreateRocket; // True if group count >= 4
    public Coordinate RocketSpawnPosition; // The tapped cell (where rocket appears)

    public BlastResult()
    {
        BlastedCoordinates = new List<Coordinate>();
        DamagedObstacles = new List<Coordinate>();
        DestroyedObstacles = new List<Coordinate>();
    }
}
