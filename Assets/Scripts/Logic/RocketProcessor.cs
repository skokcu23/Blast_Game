using System;
using System.Collections.Generic;

/// <summary>
/// Handles all rocket operations: creation, single explosion, and combo explosion.
/// Pure C# — no Unity dependency.
///
/// Chain Reaction Invariant:
///   When TracePath encounters another rocket, it adds the coordinate to
///   TriggeredRockets but NEVER removes it from the board. Only ExplodeRocket()
///   removes a rocket. This allows GameSession to process chains via a queue.
///
/// Combo Design:
///   A combo fires two independent rockets (H and V) from the same origin.
///   Each direction traces 3 parallel paths with its OWN processed set.
///   The 3×3 center is the natural intersection — cells there get hit by both:
///     Cube/Box/Stone (1HP): destroyed by first direction, second sees empty
///     Vase (2HP): damaged by first direction (2→1), destroyed by second (1→0)
/// </summary>
public class RocketProcessor
{
    private readonly Random _rand;

    public RocketProcessor() { _rand = new Random(); }
    public RocketProcessor(int seed) { _rand = new Random(seed); }

    // ==========================================
    // CREATION
    // ==========================================

    /// <summary>
    /// Create a rocket at the tapped cell from a group of 4+ matched cubes.
    /// Randomly chooses horizontal or vertical. Returns null if group is too small.
    /// </summary>
    public RocketCreationData CreateRocket(Board board, Coordinate tappedCell, List<Coordinate> matchedCells)
    {
        if (matchedCells.Count < 4)
            return null;

        string rocketId = _rand.Next(0, 2) == 0
            ? ItemIds.HorizontalRocket
            : ItemIds.VerticalRocket;

        board.SetItem(tappedCell, ItemFactory.CreateItem(rocketId));

        return new RocketCreationData
        {
            SpawnPosition = tappedCell,
            RocketId = rocketId,
            SourceCoordinates = matchedCells
        };
    }

    /// <summary>Check if a match count qualifies for rocket creation.</summary>
    public bool QualifiesForRocket(int matchCount) => matchCount >= 4;

    // ==========================================
    // SINGLE ROCKET EXPLOSION
    // ==========================================

    /// <summary>
    /// Explode a single rocket at the given coordinate.
    /// Traces two paths (opposite directions) destroying cubes and damaging obstacles.
    /// Returns null if the cell doesn't contain a rocket.
    /// </summary>
    public RocketExplosionData ExplodeRocket(Board board, Coordinate rocketCoord)
    {
        GridItem rocketItem = board.GetItem(rocketCoord);
        if (!rocketItem.IsRocket)
            return null;

        bool isHorizontal = rocketItem.Id == ItemIds.HorizontalRocket;
        string rocketId = rocketItem.Id;

        board.SetItem(rocketCoord, ItemFactory.CreateEmpty());

        HashSet<Coordinate> triggeredSet = new HashSet<Coordinate>();

        RocketExplosionData data = new RocketExplosionData
        {
            Origin = rocketCoord,
            IsHorizontal = isHorizontal,
            RocketId = rocketId
        };

        if (isHorizontal)
        {
            TracePath(board, rocketCoord, -1, 0, data.PathA, data, triggeredSet);
            TracePath(board, rocketCoord, 1, 0, data.PathB, data, triggeredSet);
        }
        else
        {
            TracePath(board, rocketCoord, 0, -1, data.PathA, data, triggeredSet);
            TracePath(board, rocketCoord, 0, 1, data.PathB, data, triggeredSet);
        }

        return data;
    }

    // ==========================================
    // ROCKET-ROCKET COMBO
    // ==========================================

    /// <summary>
    /// Fire two independent rockets (H and V) from the same origin.
    /// Returns [0]=horizontal explosion, [1]=vertical explosion.
    ///
    /// Each direction uses its own processed set, so the 3×3 intersection
    /// area gets hit by both directions — vases take 2 damage total.
    /// </summary>
    public List<RocketExplosionData> ProcessCombo(
        Board board, Coordinate tappedRocket, List<Coordinate> adjacentRockets)
    {
        List<RocketExplosionData> explosions = new List<RocketExplosionData>();

        // 1. Remove all combo participant rockets from the board
        board.SetItem(tappedRocket, ItemFactory.CreateEmpty());
        foreach (var adj in adjacentRockets)
            board.SetItem(adj, ItemFactory.CreateEmpty());

        HashSet<Coordinate> removedRockets = new HashSet<Coordinate>();
        removedRockets.Add(tappedRocket);
        foreach (var adj in adjacentRockets)
            removedRockets.Add(adj);

        HashSet<Coordinate> triggeredSet = new HashSet<Coordinate>();

        // 2. Horizontal explosion — 3 parallel rows
        HashSet<Coordinate> processedH = new HashSet<Coordinate>(removedRockets);

        RocketExplosionData horizData = new RocketExplosionData
        {
            Origin = tappedRocket,
            IsHorizontal = true,
            RocketId = ItemIds.HorizontalRocket,
            ParallelPathsA = new List<List<Coordinate>>(),
            ParallelPathsB = new List<List<Coordinate>>()
        };

        for (int dy = -1; dy <= 1; dy++)
        {
            Coordinate rowOrigin = new Coordinate(tappedRocket.x, tappedRocket.y + dy);

            List<Coordinate> pathLeft = new List<Coordinate>();
            TracePathCombo(board, rowOrigin, -1, 0, pathLeft, horizData, processedH, triggeredSet);
            horizData.ParallelPathsA.Add(pathLeft);

            List<Coordinate> pathRight = new List<Coordinate>();
            TracePathCombo(board, rowOrigin, 1, 0, pathRight, horizData, processedH, triggeredSet);
            horizData.ParallelPathsB.Add(pathRight);
        }

        explosions.Add(horizData);

        // 3. Vertical explosion — 3 parallel columns (own processed set)
        HashSet<Coordinate> processedV = new HashSet<Coordinate>(removedRockets);

        RocketExplosionData vertData = new RocketExplosionData
        {
            Origin = tappedRocket,
            IsHorizontal = false,
            RocketId = ItemIds.VerticalRocket,
            ParallelPathsA = new List<List<Coordinate>>(),
            ParallelPathsB = new List<List<Coordinate>>()
        };

        for (int dx = -1; dx <= 1; dx++)
        {
            Coordinate colOrigin = new Coordinate(tappedRocket.x + dx, tappedRocket.y);

            List<Coordinate> pathDown = new List<Coordinate>();
            TracePathCombo(board, colOrigin, 0, -1, pathDown, vertData, processedV, triggeredSet);
            vertData.ParallelPathsA.Add(pathDown);

            List<Coordinate> pathUp = new List<Coordinate>();
            TracePathCombo(board, colOrigin, 0, 1, pathUp, vertData, processedV, triggeredSet);
            vertData.ParallelPathsB.Add(pathUp);
        }

        explosions.Add(vertData);

        // 4. Compute ComboAreaCleared — 3×3 cells now empty on the board
        List<Coordinate> areaCleared = new List<Coordinate>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Coordinate cell = new Coordinate(tappedRocket.x + dx, tappedRocket.y + dy);
                if (!board.IsValidCoordinate(cell.x, cell.y)) continue;
                if (board.GetItem(cell).IsEmpty)
                    areaCleared.Add(cell);
            }
        }

        horizData.ComboAreaCleared = areaCleared;
        vertData.ComboAreaCleared = areaCleared;

        return explosions;
    }

    // ==========================================
    // PATH TRACING
    // ==========================================

    /// <summary>
    /// Trace a single-width path for a normal (non-combo) rocket.
    /// Starts one cell past the origin in the given direction.
    /// </summary>
    private void TracePath(
        Board board, Coordinate origin, int dx, int dy,
        List<Coordinate> path, RocketExplosionData data,
        HashSet<Coordinate> triggeredSet)
    {
        int x = origin.x + dx;
        int y = origin.y + dy;

        while (board.IsValidCoordinate(x, y))
        {
            Coordinate cell = new Coordinate(x, y);
            path.Add(cell);

            GridItem item = board.GetItem(cell);

            if (item.IsEmpty)
            {
                // Pass through
            }
            else if (item.IsRocket)
            {
                if (triggeredSet.Add(cell))
                    data.TriggeredRockets.Add(cell);
            }
            else if (item.IsCube)
            {
                data.DestroyedCubes.Add(cell);
                board.SetItem(cell, ItemFactory.CreateEmpty());
            }
            else if (item.IsObstacle)
            {
                ApplyObstacleDamage(board, cell, item, DamageSource.Rocket, data);
            }

            x += dx;
            y += dy;
        }
    }

    /// <summary>
    /// Trace a path for one row/column of a combo.
    /// Starts AT the origin cell (not origin + direction) so every cell
    /// in the 3×3 area is visited by both H and V directions.
    /// </summary>
    private void TracePathCombo(
        Board board, Coordinate origin, int dx, int dy,
        List<Coordinate> path, RocketExplosionData data,
        HashSet<Coordinate> alreadyProcessed, HashSet<Coordinate> triggeredSet)
    {
        int x = origin.x;
        int y = origin.y;

        while (board.IsValidCoordinate(x, y))
        {
            Coordinate cell = new Coordinate(x, y);
            path.Add(cell);

            if (!alreadyProcessed.Contains(cell))
            {
                alreadyProcessed.Add(cell);

                GridItem item = board.GetItem(cell);

                if (item.IsRocket)
                {
                    if (triggeredSet.Add(cell))
                        data.TriggeredRockets.Add(cell);
                }
                else if (item.IsCube)
                {
                    data.DestroyedCubes.Add(cell);
                    board.SetItem(cell, ItemFactory.CreateEmpty());
                }
                else if (item.IsObstacle)
                {
                    ApplyObstacleDamage(board, cell, item, DamageSource.Rocket, data);
                }
            }

            x += dx;
            y += dy;
        }
    }

    // ==========================================
    // DAMAGE HELPER
    // ==========================================

    private void ApplyObstacleDamage(
        Board board, Coordinate cell, GridItem item,
        DamageSource source, RocketExplosionData data)
    {
        string originalId = item.Id;
        var dmgResult = DamageResolver.TryDamage(item, source);

        if (dmgResult.WasApplied)
        {
            if (dmgResult.WasDestroyed)
            {
                board.SetItem(cell, ItemFactory.CreateEmpty());
                data.DestroyedObstacles.Add(cell);
                data.DestroyedObstacleInfos.Add(new DestroyedObstacleInfo(cell, originalId));
            }
            else
            {
                data.DamagedObstacles.Add(cell);
            }
        }
    }
}
