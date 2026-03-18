using System;
using System.Collections.Generic;

/// <summary>
/// Handles all rocket operations. Pure C# — no Unity dependency.
///
/// ╔══════════════════════════════════════════════════════════════╗
/// ║ CRITICAL INVARIANT:                                        ║
/// ║ When TracePath/ProcessCellDamage encounters another rocket, ║
/// ║ it adds the coordinate to TriggeredRockets but NEVER       ║
/// ║ removes it from the board. Only ExplodeRocket() removes    ║
/// ║ a rocket — the one it is currently exploding.              ║
/// ║                                                            ║
/// ║ This ensures the Orchestrator's queue loop can find and    ║
/// ║ explode triggered rockets. Violating this breaks ALL       ║
/// ║ chain reactions.                                           ║
/// ╚══════════════════════════════════════════════════════════════╝
/// </summary>
public class RocketProcessor
{
    private readonly Random _rand;

    public RocketProcessor() { _rand = new Random(); }
    public RocketProcessor(int seed) { _rand = new Random(seed); }

    // ==========================================
    // CREATION (from 3A, unchanged)
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
    // SINGLE ROCKET EXPLOSION
    // ==========================================

    /// <summary>
    /// Explode a single rocket at rocketCoord.
    /// Removes THIS rocket from the board, then traces two paths.
    /// Returns null if the cell doesn't contain a rocket.
    /// </summary>
    public RocketExplosionData ExplodeRocket(Board board, Coordinate rocketCoord)
    {
        GridItem rocketItem = board.GetItem(rocketCoord);
        if (!rocketItem.IsRocket)
            return null;

        bool isHorizontal = rocketItem.Id == ItemIds.HorizontalRocket;
        string rocketId = rocketItem.Id;

        // Remove THIS rocket (the one being exploded RIGHT NOW)
        board.SetItem(rocketCoord, ItemFactory.CreateEmpty());

        // Dedup set prevents the same rocket appearing in TriggeredRockets twice
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
    /// 3×3 area explosion + rockets in both horizontal and vertical directions.
    /// Returns two RocketExplosionData: [0]=horizontal, [1]=vertical.
    /// Animated in parallel by the Orchestrator.
    /// </summary>
    public List<RocketExplosionData> ProcessCombo(
        Board board, Coordinate tappedRocket, List<Coordinate> adjacentRockets)
    {
        List<RocketExplosionData> explosions = new List<RocketExplosionData>();

        // 1. Remove ALL involved rockets (tapped + adjacent)
        //    These are the combo participants — they merge, not chain-trigger.
        board.SetItem(tappedRocket, ItemFactory.CreateEmpty());
        foreach (var adj in adjacentRockets)
            board.SetItem(adj, ItemFactory.CreateEmpty());

        // 2. Process 3×3 area — shared state across both explosion data
        HashSet<Coordinate> processed = new HashSet<Coordinate>();
        HashSet<Coordinate> triggeredSet = new HashSet<Coordinate>();

        List<Coordinate> destroyedCubes3x3 = new List<Coordinate>();
        List<Coordinate> damagedObstacles3x3 = new List<Coordinate>();
        List<Coordinate> destroyedObstacles3x3 = new List<Coordinate>();
        List<DestroyedObstacleInfo> destroyedInfos3x3 = new List<DestroyedObstacleInfo>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Coordinate cell = new Coordinate(tappedRocket.x + dx, tappedRocket.y + dy);
                if (!board.IsValidCoordinate(cell.x, cell.y))
                    continue;

                processed.Add(cell);
                ProcessCellDamage(board, cell, destroyedCubes3x3, damagedObstacles3x3,
                    destroyedObstacles3x3, destroyedInfos3x3, triggeredSet);
            }
        }

        // In ProcessCombo, replace steps 3 and 4:

        // 3. Horizontal explosion — 3 parallel rows (y-1, y, y+1)
        RocketExplosionData horizData = new RocketExplosionData
        {
            Origin = tappedRocket,
            IsHorizontal = true,
            RocketId = ItemIds.HorizontalRocket,
            DestroyedCubes = new List<Coordinate>(destroyedCubes3x3),
            DamagedObstacles = new List<Coordinate>(damagedObstacles3x3),
            DestroyedObstacles = new List<Coordinate>(destroyedObstacles3x3),
            DestroyedObstacleInfos = new List<DestroyedObstacleInfo>(destroyedInfos3x3),
            TriggeredRockets = new List<Coordinate>(triggeredSet)
        };

        // 3 rows going LEFT
        for (int dy = -1; dy <= 1; dy++)
        {
            Coordinate rowOrigin = new Coordinate(tappedRocket.x, tappedRocket.y + dy);
            TracePathCombo(board, rowOrigin, -1, 0, horizData.PathA, horizData, processed, triggeredSet);
        }
        // 3 rows going RIGHT
        for (int dy = -1; dy <= 1; dy++)
        {
            Coordinate rowOrigin = new Coordinate(tappedRocket.x, tappedRocket.y + dy);
            TracePathCombo(board, rowOrigin, 1, 0, horizData.PathB, horizData, processed, triggeredSet);
        }
        explosions.Add(horizData);

        // 4. Vertical explosion — 3 parallel columns (x-1, x, x+1)
        RocketExplosionData vertData = new RocketExplosionData
        {
            Origin = tappedRocket,
            IsHorizontal = false,
            RocketId = ItemIds.VerticalRocket
        };

        // 3 columns going DOWN
        for (int dx = -1; dx <= 1; dx++)
        {
            Coordinate colOrigin = new Coordinate(tappedRocket.x + dx, tappedRocket.y);
            TracePathCombo(board, colOrigin, 0, -1, vertData.PathA, vertData, processed, triggeredSet);
        }
        // 3 columns going UP
        for (int dx = -1; dx <= 1; dx++)
        {
            Coordinate colOrigin = new Coordinate(tappedRocket.x + dx, tappedRocket.y);
            TracePathCombo(board, colOrigin, 0, 1, vertData.PathB, vertData, processed, triggeredSet);
        }
        explosions.Add(vertData);

        return explosions;
    }

    // ==========================================
    // PATH TRACING
    // ==========================================

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
                // *** CRITICAL: DO NOT remove from board ***
                // ExplodeRocket() handles removal when the queue processes it.
                if (triggeredSet.Add(cell)) // Add returns false if already present
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

    private void TracePathCombo(
        Board board, Coordinate origin, int dx, int dy,
        List<Coordinate> path, RocketExplosionData data,
        HashSet<Coordinate> alreadyProcessed, HashSet<Coordinate> triggeredSet)
    {
        int x = origin.x + dx;
        int y = origin.y + dy;

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
    // CELL DAMAGE HELPERS
    // ==========================================

    /// <summary>
    /// Process damage for 3×3 combo area. Uses separate output lists.
    /// Rockets are NOT removed — only added to triggeredSet.
    /// </summary>
    private void ProcessCellDamage(
        Board board, Coordinate cell,
        List<Coordinate> destroyedCubes,
        List<Coordinate> damagedObstacles,
        List<Coordinate> destroyedObstacles,
        List<DestroyedObstacleInfo> destroyedInfos,
        HashSet<Coordinate> triggeredSet)
    {
        GridItem item = board.GetItem(cell);
        if (item.IsEmpty) return;

        if (item.IsRocket)
        {
            // *** CRITICAL: DO NOT remove from board ***
            triggeredSet.Add(cell);
            return;
        }

        if (item.IsCube)
        {
            destroyedCubes.Add(cell);
            board.SetItem(cell, ItemFactory.CreateEmpty());
            return;
        }

        if (item.IsObstacle)
        {
            string originalId = item.Id;
            var dmgResult = DamageResolver.TryDamage(item, DamageSource.Combo);

            if (dmgResult.WasApplied)
            {
                if (dmgResult.WasDestroyed)
                {
                    board.SetItem(cell, ItemFactory.CreateEmpty());
                    destroyedObstacles.Add(cell);
                    destroyedInfos.Add(new DestroyedObstacleInfo(cell, originalId));
                }
                else
                {
                    damagedObstacles.Add(cell);
                }
            }
        }
    }

    /// <summary>
    /// Apply damage to an obstacle, recording results in the explosion data.
    /// Shared between TracePath and TracePathCombo.
    /// </summary>
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
