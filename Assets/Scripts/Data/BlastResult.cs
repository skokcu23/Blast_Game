using System.Collections.Generic;

/// <summary>
/// Complete result of a cube group blast operation.
/// Produced by ClassicMatchStrategy, consumed by GameSession and the View layer.
///
/// Contains everything needed to animate the blast and update game state:
/// which cubes were destroyed, which obstacles were damaged/destroyed,
/// and whether a rocket should be created.
/// </summary>
public class BlastResult
{
    /// <summary>Whether this blast was valid (matched group found).</summary>
    public bool IsValid;

    /// <summary>Coordinates of all cubes removed by this blast.</summary>
    public List<Coordinate> BlastedCoordinates;

    /// <summary>Obstacles that took damage but survived (View shakes them).</summary>
    public List<Coordinate> DamagedObstacles;

    /// <summary>Obstacles that were destroyed by this blast (View pops them).</summary>
    public List<Coordinate> DestroyedObstacles;

    /// <summary>Type info for destroyed obstacles (needed by ObstacleGoalTracker).</summary>
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos;

    /// <summary>True if the matched group had 4+ cubes (qualifies for rocket).</summary>
    public bool ShouldCreateRocket;

    /// <summary>Moves remaining after this blast (legacy, tracked by GameSession).</summary>
    public int RemainingMoves;

    /// <summary>Where to place the rocket (the tapped cell). Only valid if ShouldCreateRocket.</summary>
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
/// Records the type and position of a destroyed obstacle.
/// Captured before the board cell is cleared to Empty, so the
/// ObstacleGoalTracker can decrement the correct type's count.
/// </summary>
public struct DestroyedObstacleInfo
{
    /// <summary>Where the obstacle was on the board.</summary>
    public Coordinate Position;

    /// <summary>The obstacle's ItemId before destruction (e.g., ItemIds.Box).</summary>
    public string ObstacleId;

    public DestroyedObstacleInfo(Coordinate position, string obstacleId)
    {
        Position = position;
        ObstacleId = obstacleId;
    }
}
