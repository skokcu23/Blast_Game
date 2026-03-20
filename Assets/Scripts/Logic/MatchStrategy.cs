using System.Collections.Generic;

/// <summary>
/// Strategy Pattern interface for match finding and blast execution.
///
/// Current implementation: ClassicMatchStrategy (Match-2, flood-fill).
/// Swap implementations to change game rules without touching Board, GameSession, or the View.
/// </summary>
public interface MatchStrategy
{
    /// <summary>
    /// Find all coordinates that form a valid match group starting from the tapped cell.
    /// Returns an empty list if no valid match exists (less than 2 adjacent same-color cubes).
    /// </summary>
    List<Coordinate> FindMatches(Board board, Coordinate tap);

    /// <summary>
    /// Execute the blast: clear matched cubes from the board and deal adjacent damage to obstacles.
    /// Returns a BlastResult with all data needed for animation and goal tracking.
    /// </summary>
    BlastResult Blast(Board board, List<Coordinate> matches, Coordinate tappedCell);
}
