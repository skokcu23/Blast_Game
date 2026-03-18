using System.Collections.Generic;

/// <summary>
/// DTO: Complete result of a blast operation.
/// Passed from Logic → Orchestrator → View.
/// </summary>
public class BlastResult
{
    public bool IsValid;
    public List<Coordinate> BlastedCoordinates;
    public List<Coordinate> DamagedObstacles;       // Survived but took damage
    public List<Coordinate> DestroyedObstacles;      // Killed this blast
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos; // Type info for GoalTracker
    public int RemainingMoves;
    public bool ShouldCreateRocket;
    public Coordinate RocketSpawnPosition;

    public BlastResult()
    {
        BlastedCoordinates = new List<Coordinate>();
        DamagedObstacles = new List<Coordinate>();
        DestroyedObstacles = new List<Coordinate>();
        DestroyedObstacleInfos = new List<DestroyedObstacleInfo>();
    }
}

/// <summary>
/// Records what type of obstacle was destroyed and where.
/// Needed because the board cell is already cleared to Empty
/// by the time the GoalTracker needs to update counts.
/// </summary>
public struct DestroyedObstacleInfo
{
    public Coordinate Position;
    public string ObstacleId;

    public DestroyedObstacleInfo(Coordinate position, string obstacleId)
    {
        Position = position;
        ObstacleId = obstacleId;
    }
}
