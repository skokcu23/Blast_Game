using System.Collections.Generic;

/// <summary>
/// Strategy Pattern: Defines how matches are found and blasted.
/// Swap implementations for different game rules without touching Board or Orchestrator.
/// </summary>
public interface MatchStrategy
{
    /// <summary>
    /// Find all coordinates that form a valid match group from the tapped cell.
    /// Returns empty list if no valid match exists.
    /// </summary>
    List<Coordinate> FindMatches(Board board, Coordinate tap);

    /// <summary>
    /// Execute the blast: clear matched cubes and deal adjacent damage to obstacles.
    /// Returns a BlastResult with full details for the Orchestrator.
    /// </summary>
    BlastResult Blast(Board board, List<Coordinate> matches, Coordinate tappedCell);
}
