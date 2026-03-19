using System;
using System.Collections.Generic;

/// <summary>
/// Handles all rocket operations. Pure C# — no Unity dependency.
///
/// ╔══════════════════════════════════════════════════════════════╗
/// ║ CRITICAL INVARIANT:                                        ║
/// ║ When TracePath encounters another rocket, it adds the      ║
/// ║ coordinate to TriggeredRockets but NEVER removes it from   ║
/// ║ the board. Only ExplodeRocket() removes a rocket.          ║
/// ╚══════════════════════════════════════════════════════════════╝
///
/// COMBO DESIGN:
/// A combo fires two independent rockets (H and V) from the same origin.
/// Each direction traces 3 parallel paths with its OWN processed set.
/// The 3×3 center area is the natural intersection — cells there get
/// hit by both directions:
///   - Cube (1HP):  H destroys → V sees empty → 1 hit total
///   - Box (1HP):   H destroys → V sees empty → 1 hit total
///   - Stone (1HP): H destroys → V sees empty → 1 hit total
///   - Vase (2HP):  H damages (2→1) → V destroys (1→0) → 2 hits total
///
/// No special-case 3×3 processing. No double-damage hacks.
/// The geometry does the work.
/// </summary>
public class RocketProcessor
{
    private readonly Random _rand;

    public RocketProcessor() { _rand = new Random(); }
    public RocketProcessor(int seed) { _rand = new Random(seed); }

    // ==========================================
    // CREATION
    // ==========================================

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

    public bool QualifiesForRocket(int matchCount) => matchCount >= 4;

    // ==========================================
    // SINGLE ROCKET EXPLOSION (unchanged)
    // ==========================================

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
    /// Two independent rockets fire from the same origin.
    /// Returns [0]=horizontal, [1]=vertical.
    ///
    /// Each direction traces 3 parallel full-width/full-height paths
    /// with its own processed set. The 3×3 center is the natural
    /// intersection where cells get hit by both directions.
    /// </summary>
    public List<RocketExplosionData> ProcessCombo(
        Board board, Coordinate tappedRocket, List<Coordinate> adjacentRockets)
    {
        List<RocketExplosionData> explosions = new List<RocketExplosionData>();

        // 1. Remove ALL combo participant rockets from the board
        board.SetItem(tappedRocket, ItemFactory.CreateEmpty());
        foreach (var adj in adjacentRockets)
            board.SetItem(adj, ItemFactory.CreateEmpty());

        // Removed rocket coords — both directions skip these (they're already empty,
        // but we add them to processed sets so path tracing doesn't try to "damage" empty cells
        // and so the origin is cleanly handled)
        HashSet<Coordinate> removedRockets = new HashSet<Coordinate>();
        removedRockets.Add(tappedRocket);
        foreach (var adj in adjacentRockets)
            removedRockets.Add(adj);

        // Shared triggered set — prevents same chain-rocket from being queued twice
        HashSet<Coordinate> triggeredSet = new HashSet<Coordinate>();

        // 2. HORIZONTAL explosion — traces 3 full rows independently
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

        // 3. VERTICAL explosion — traces 3 full columns independently
        //    Uses its OWN processed set. Cells in the 3×3 that H already
        //    damaged are NOT in processedV, so V damages them again.
        //    This is correct: two rockets cross at the center.
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

        // 4. Compute ComboAreaCleared — every 3×3 cell that is now empty
        //    on the board. Used by the View to clear visuals before
        //    projectile animation starts.
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
                // pass through
            }
            else if (item.IsRocket)
            {
                // CRITICAL: DO NOT remove from board
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
    ///
    /// IMPORTANT: Starts at the origin cell itself (not origin + direction).
    /// This ensures that every cell in the 3×3 area is visited by BOTH
    /// the horizontal and vertical traces, so obstacles take 2 damage.
    ///
    /// Uses alreadyProcessed to prevent double-damage within the same
    /// direction (left and right both start at origin — the second call
    /// finds origin already processed and skips it).
    /// </summary>
    private void TracePathCombo(
        Board board, Coordinate origin, int dx, int dy,
        List<Coordinate> path, RocketExplosionData data,
        HashSet<Coordinate> alreadyProcessed, HashSet<Coordinate> triggeredSet)
    {
        // Start AT origin, not origin + direction.
        // This is critical for 3×3 damage: the origin cells of each row/column
        // are part of the 3×3 area and must be visited by both directions.
        int x = origin.x;
        int y = origin.y;

        while (board.IsValidCoordinate(x, y))
        {
            Coordinate cell = new Coordinate(x, y);
            path.Add(cell); // Always add to path for visual traversal

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
                // Empty cells: pass through
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
