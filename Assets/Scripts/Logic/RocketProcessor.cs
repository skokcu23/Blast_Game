using System;
using System.Collections.Generic;

/// <summary>
/// Handles all rocket operations. Pure C# — no Unity dependency.
///
/// Iteration 3A: Rocket creation
/// Iteration 3B: Explosion, combo, chain reactions (methods added later)
///
/// Case Study rules for creation:
///   - Group count ≥ 4 → create rocket at tapped cell
///   - Direction is random (horizontal or vertical)
///   - Rocket appears at the location of the tapped cube
/// </summary>
public class RocketProcessor
{
    private readonly Random _rand;

    public RocketProcessor()
    {
        _rand = new Random();
    }

    // Constructor with seed for deterministic testing
    public RocketProcessor(int seed)
    {
        _rand = new Random(seed);
    }

    /// <summary>
    /// Create a rocket on the board after a group blast of ≥ 4 cubes.
    ///
    /// IMPORTANT: Call this AFTER the cubes have been cleared from the board
    /// by ClassicMatchStrategy.Blast(). This method places the new rocket
    /// at the (now empty) tapped cell.
    ///
    /// Returns a RocketCreationData for the View to animate, or null if
    /// the blast doesn't qualify for rocket creation.
    /// </summary>
    public RocketCreationData CreateRocket(Board board, Coordinate tappedCell, List<Coordinate> matchedCells)
    {
        if (matchedCells.Count < 4)
            return null;

        // Random direction
        string rocketId = _rand.Next(0, 2) == 0
            ? ItemIds.HorizontalRocket
            : ItemIds.VerticalRocket;

        // Place the rocket on the board
        GridItem rocket = ItemFactory.CreateItem(rocketId);
        board.SetItem(tappedCell, rocket);

        return new RocketCreationData
        {
            SpawnPosition = tappedCell,
            RocketId = rocketId,
            SourceCoordinates = matchedCells
        };
    }

    /// <summary>
    /// Check if a blast result qualifies for rocket creation.
    /// Convenience method for the Orchestrator.
    /// </summary>
    public bool QualifiesForRocket(int matchCount)
    {
        return matchCount >= 4;
    }

    // ==========================================
    // ITERATION 3B — Explosion methods will go here
    // ==========================================
    // public RocketExplosionData ExplodeRocket(Board board, Coordinate rocketCoord) { ... }
    // public RocketExplosionData ProcessCombo(Board board, Coordinate tappedRocket, List<Coordinate> adjacentRockets) { ... }
}
