using System.Collections.Generic;

/// <summary>
/// Single source of truth for ALL damage rules in the game.
/// Pure C# — no Unity dependencies, fully NUnit-testable.
///
/// Case Study damage rules:
/// ┌──────────┬───────┬────────┬───────┬─────────────────────────┐
/// │ Obstacle │ Blast │ Rocket │  HP   │ Special                 │
/// ├──────────┼───────┼────────┼───────┼─────────────────────────┤
/// │ Box      │  Yes  │  Yes   │  1    │ —                       │
/// │ Stone    │  No   │  Yes   │  1    │ Immune to blast         │
/// │ Vase     │  Yes  │  Yes   │  2    │ Max 1 damage per blast  │
/// └──────────┴───────┴────────┴───────┴─────────────────────────┘
///
/// Usage:
///   var result = DamageResolver.TryDamage(item, DamageSource.Blast);
///   // or with vase-cap tracking:
///   DamageResolver.ProcessAdjacentDamage(board, blastCells, DamageSource.Blast, blastResult);
/// </summary>
public static class DamageResolver
{
    /// <summary>
    /// Can this damage source hurt this item at all?
    /// Does NOT apply damage — just checks the rule table.
    /// </summary>
    public static bool CanDamage(GridItem item, DamageSource source)
    {
        if (item == null || item.IsEmpty || !item.IsObstacle || !item.IsAlive)
            return false;

        return item.Id switch
        {
            ItemIds.Box   => source == DamageSource.Blast || source == DamageSource.Rocket || source == DamageSource.Combo,
            ItemIds.Stone => source == DamageSource.Rocket || source == DamageSource.Combo, // Immune to Blast
            ItemIds.Vase  => source == DamageSource.Blast || source == DamageSource.Rocket || source == DamageSource.Combo,
            _ => false
        };
    }

    /// <summary>
    /// Attempt to deal 1 damage to an item from the given source.
    /// Returns a DamageResult describing what happened.
    /// Does NOT handle the vase-per-blast cap — that's the caller's job via ProcessAdjacentDamage.
    /// </summary>
    public static DamageResult TryDamage(GridItem item, DamageSource source)
    {
        if (!CanDamage(item, source))
            return DamageResult.Immune();

        item.TakeDamage(1);
        return DamageResult.Applied(item.Health);
    }

    /// <summary>
    /// Process damage to all obstacles adjacent to a set of blast/explosion cells.
    /// Handles the Vase rule: max 1 damage per blast group.
    ///
    /// This is the main entry point for blast-adjacent damage.
    /// For rocket line-damage, call TryDamage directly per cell the rocket passes over.
    /// </summary>
    public static void ProcessAdjacentDamage(
        Board board,
        List<Coordinate> sourceCells,
        DamageSource source,
        BlastResult result)
    {
        // Track which obstacles we've already damaged this operation
        // (enforces Vase's "max 1 damage per blast group" rule)
        HashSet<Coordinate> alreadyDamaged = new HashSet<Coordinate>();

        foreach (var cell in sourceCells)
        {
            ProcessNeighborDamage(board, cell, source, alreadyDamaged, result);
        }
    }

    /// <summary>
    /// Damage obstacles in a single cell (not adjacent — the cell itself).
    /// Used by rockets passing over cells.
    /// </summary>
    public static DamageResult DamageAt(Board board, Coordinate coord, DamageSource source, BlastResult result)
    {
        GridItem item = board.GetItem(coord);
        string originalId = item.Id; // Capture BEFORE damage (needed for GoalTracker)

        var damageResult = TryDamage(item, source);

        if (!damageResult.WasApplied)
            return damageResult;

        if (damageResult.WasDestroyed)
        {
            board.SetItem(coord, ItemFactory.CreateEmpty());
            result.DestroyedObstacles.Add(coord);
            result.DestroyedObstacleInfos.Add(new DestroyedObstacleInfo(coord, originalId));
        }
        else
        {
            result.DamagedObstacles.Add(coord);
        }

        return damageResult;
    }

    // --- Private ---

    private static void ProcessNeighborDamage(
        Board board,
        Coordinate center,
        DamageSource source,
        HashSet<Coordinate> alreadyDamaged,
        BlastResult result)
    {
        Coordinate[] neighbors =
        {
            new Coordinate(center.x, center.y + 1),
            new Coordinate(center.x, center.y - 1),
            new Coordinate(center.x + 1, center.y),
            new Coordinate(center.x - 1, center.y)
        };

        foreach (var neighbor in neighbors)
        {
            if (!board.IsValidCoordinate(neighbor.x, neighbor.y))
                continue;

            // Already damaged this obstacle in this operation
            if (alreadyDamaged.Contains(neighbor))
                continue;

            GridItem item = board.GetItem(neighbor);

            if (!CanDamage(item, source))
                continue;

            // Capture ID BEFORE damage (board cell gets cleared on destroy)
            string originalId = item.Id;

            // Mark as damaged BEFORE applying (prevents double-damage from multiple adjacent blasts)
            alreadyDamaged.Add(neighbor);

            var damageResult = TryDamage(item, source);

            if (damageResult.WasDestroyed)
            {
                board.SetItem(neighbor, ItemFactory.CreateEmpty());
                result.DestroyedObstacles.Add(neighbor);
                result.DestroyedObstacleInfos.Add(new DestroyedObstacleInfo(neighbor, originalId));
            }
            else
            {
                result.DamagedObstacles.Add(neighbor);
            }
        }
    }
}
