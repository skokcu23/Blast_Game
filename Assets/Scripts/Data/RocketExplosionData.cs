using System.Collections.Generic;

/// <summary>
/// DTO: Describes one rocket explosion for the View to animate.
///
/// A rocket splits into two "projectiles" moving in opposite directions.
/// PathA/PathB are ordered lists of cells each projectile passes through.
///
/// TriggeredRockets: rockets hit by this explosion that the queue should process next.
/// IMPORTANT: These rockets are still ON the board — ExplodeRocket() handles removal.
/// </summary>
public class RocketExplosionData
{
    public Coordinate Origin;
    public bool IsHorizontal;
    public string RocketId;

    /// <summary>Ordered path of first projectile (left or down)</summary>
    public List<Coordinate> PathA;

    /// <summary>Ordered path of second projectile (right or up)</summary>
    public List<Coordinate> PathB;

    /// <summary>Cubes destroyed by this explosion</summary>
    public List<Coordinate> DestroyedCubes;

    /// <summary>Obstacles damaged but survived</summary>
    public List<Coordinate> DamagedObstacles;

    /// <summary>Obstacles destroyed by this explosion</summary>
    public List<Coordinate> DestroyedObstacles;

    /// <summary>Type info for GoalTracker (captured before board cell cleared)</summary>
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos;

    /// <summary>
    /// Rockets hit by this explosion — still on the board.
    /// The Orchestrator enqueues these for chain reaction processing.
    /// </summary>
    public List<Coordinate> TriggeredRockets;

    public RocketExplosionData()
    {
        PathA = new List<Coordinate>();
        PathB = new List<Coordinate>();
        DestroyedCubes = new List<Coordinate>();
        DamagedObstacles = new List<Coordinate>();
        DestroyedObstacles = new List<Coordinate>();
        DestroyedObstacleInfos = new List<DestroyedObstacleInfo>();
        TriggeredRockets = new List<Coordinate>();
    }
}
