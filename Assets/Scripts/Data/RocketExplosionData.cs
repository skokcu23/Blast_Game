using System.Collections.Generic;

/// <summary>
/// Describes one rocket explosion for the View to animate.
///
/// Two modes (check IsCombo):
///   Single:  PathA/PathB contain one path each (2 projectiles, opposite directions).
///   Combo:   ParallelPathsA/ParallelPathsB contain 3 paths each (6 projectiles total).
///
/// Damage lists are aggregated across all paths for this explosion.
/// </summary>
public class RocketExplosionData
{
    /// <summary>Grid cell where the rocket was before exploding.</summary>
    public Coordinate Origin;

    /// <summary>True if this is a horizontal rocket (projectiles go left/right).</summary>
    public bool IsHorizontal;

    /// <summary>The rocket's ItemId (e.g., ItemIds.HorizontalRocket).</summary>
    public string RocketId;

    // --- Single rocket paths ---

    /// <summary>Path in the negative direction (left or down). One coordinate per cell traversed.</summary>
    public List<Coordinate> PathA;

    /// <summary>Path in the positive direction (right or up).</summary>
    public List<Coordinate> PathB;

    // --- Combo rocket paths (3 parallel per direction) ---

    /// <summary>3 parallel paths in the negative direction. Null for single rockets.</summary>
    public List<List<Coordinate>> ParallelPathsA;

    /// <summary>3 parallel paths in the positive direction. Null for single rockets.</summary>
    public List<List<Coordinate>> ParallelPathsB;

    // --- Damage data (aggregate of all paths) ---

    /// <summary>Cubes destroyed by this explosion.</summary>
    public List<Coordinate> DestroyedCubes;

    /// <summary>Obstacles that took damage but survived.</summary>
    public List<Coordinate> DamagedObstacles;

    /// <summary>Obstacles destroyed by this explosion.</summary>
    public List<Coordinate> DestroyedObstacles;

    /// <summary>Type info for destroyed obstacles (for ObstacleGoalTracker).</summary>
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos;

    /// <summary>Other rockets hit by this explosion (queued for chain reaction).</summary>
    public List<Coordinate> TriggeredRockets;

    /// <summary>
    /// Combo only: 3×3 area cells that are now empty on the board.
    /// The View clears these visuals before projectile animation starts.
    /// Null for single rockets.
    /// </summary>
    public List<Coordinate> ComboAreaCleared;

    /// <summary>True if this is a combo explosion (has parallel paths).</summary>
    public bool IsCombo => ParallelPathsA != null && ParallelPathsA.Count > 0;

    public RocketExplosionData()
    {
        PathA = new List<Coordinate>();
        PathB = new List<Coordinate>();
        ParallelPathsA = null;
        ParallelPathsB = null;
        DestroyedCubes = new List<Coordinate>();
        DamagedObstacles = new List<Coordinate>();
        DestroyedObstacles = new List<Coordinate>();
        DestroyedObstacleInfos = new List<DestroyedObstacleInfo>();
        TriggeredRockets = new List<Coordinate>();
        ComboAreaCleared = null;
    }
}
