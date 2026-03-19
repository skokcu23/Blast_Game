using System.Collections.Generic;

/// <summary>
/// DTO: Describes one rocket explosion for the View to animate.
///
/// Two modes:
///   Single rocket: uses PathA/PathB (1 projectile each direction)
///   Combo rocket: uses ParallelPathsA/ParallelPathsB (3 projectiles each direction)
///
/// Check IsCombo to determine which paths to read.
///
/// Damage data (DestroyedCubes, DamagedObstacles, etc.) is the same
/// regardless of mode — it's the aggregate of all paths.
/// </summary>
public class RocketExplosionData
{
    public Coordinate Origin;
    public bool IsHorizontal;
    public string RocketId;

    // --- Single rocket paths (1 per direction) ---

    /// <summary>Single path: left or down</summary>
    public List<Coordinate> PathA;

    /// <summary>Single path: right or up</summary>
    public List<Coordinate> PathB;

    // --- Combo rocket paths (3 per direction) ---

    /// <summary>
    /// Combo: 3 parallel paths going left/down.
    /// Each inner list is one row/column's path, ordered by cell.
    /// For horizontal: [row y-1], [row y], [row y+1]
    /// For vertical: [col x-1], [col x], [col x+1]
    /// </summary>
    public List<List<Coordinate>> ParallelPathsA;

    /// <summary>
    /// Combo: 3 parallel paths going right/up.
    /// Same structure as ParallelPathsA but opposite direction.
    /// </summary>
    public List<List<Coordinate>> ParallelPathsB;

    // --- Damage data (same for both modes) ---

    public List<Coordinate> DestroyedCubes;
    public List<Coordinate> DamagedObstacles;
    public List<Coordinate> DestroyedObstacles;
    public List<DestroyedObstacleInfo> DestroyedObstacleInfos;
    public List<Coordinate> TriggeredRockets;

    /// <summary>
    /// True if this explosion uses parallel paths (combo).
    /// The View checks this to decide how many projectiles to spawn.
    /// </summary>
    public bool IsCombo => ParallelPathsA != null && ParallelPathsA.Count > 0;

    public RocketExplosionData()
    {
        PathA = new List<Coordinate>();
        PathB = new List<Coordinate>();
        ParallelPathsA = null; // null = not a combo
        ParallelPathsB = null;
        DestroyedCubes = new List<Coordinate>();
        DamagedObstacles = new List<Coordinate>();
        DestroyedObstacles = new List<Coordinate>();
        DestroyedObstacleInfos = new List<DestroyedObstacleInfo>();
        TriggeredRockets = new List<Coordinate>();
    }
}
