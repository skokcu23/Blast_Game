using System.Collections.Generic;

/// <summary>
/// DTO: Describes one rocket explosion for the View to animate.
///
/// Two modes:
///   Single rocket: PathA/PathB (1 projectile each direction)
///   Combo rocket:  ParallelPathsA/ParallelPathsB (3 projectiles each direction)
///
/// Check IsCombo to determine which paths to read.
///
/// Damage data (DestroyedCubes, DamagedObstacles, etc.) is the aggregate
/// of all paths for this direction's explosion.
/// </summary>
public class RocketExplosionData
{
    public Coordinate Origin;
    public bool IsHorizontal;
    public string RocketId;

    // --- Single rocket paths ---
    public List<Coordinate> PathA;
    public List<Coordinate> PathB;

    // --- Combo rocket paths (3 per direction) ---
    public List<List<Coordinate>> ParallelPathsA;
    public List<List<Coordinate>> ParallelPathsB;

    // --- Damage data ---
    public List<Coordinate> DestroyedCubes;
    public List<Coordinate> DamagedObstacles;
    public List<Coordinate> DestroyedObstacles;
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos;
    public List<Coordinate> TriggeredRockets;

    /// <summary>
    /// Combo only: coordinates in the 3×3 area that are now empty on the board.
    /// The View clears these visuals BEFORE projectile animation starts.
    /// Includes: removed rockets, destroyed cubes, destroyed obstacles.
    /// Does NOT include: surviving damaged obstacles (e.g., vases damaged by
    /// one direction but killed by the other — the killing direction owns that).
    ///
    /// Null for single rockets.
    /// </summary>
    public List<Coordinate> ComboAreaCleared;

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
