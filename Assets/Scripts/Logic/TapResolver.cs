using System.Collections.Generic;

/// <summary>
/// Determines what should happen when a player taps a cell.
/// Pure C# — no Unity dependency.
///
/// Priority order:
///   1. Rocket with adjacent rocket  → RocketCombo
///   2. Rocket without adjacent rocket → ExplodeRocket
///   3. Cube with valid match group (≥ 2) → BlastGroup
///   4. Everything else → None
///
/// The Orchestrator calls Resolve() once per tap, then dispatches
/// to the appropriate system based on the result.
/// </summary>
public class TapResolver
{
    /// <summary>
    /// Resolve what a tap at the given coordinate means.
    /// Returns a TapResult containing the action and any pre-computed data.
    /// </summary>
    public TapResult Resolve(Board board, Coordinate coord, MatchStrategy matchStrategy)
    {
        GridItem item = board.GetItem(coord);

        // --- Empty or obstacle: no action ---
        if (item.IsEmpty)
            return TapResult.NoAction();

        if (item.IsObstacle)
            return TapResult.NoAction();

        // --- Rocket: check for combo first ---
        if (item.IsRocket)
        {
            List<Coordinate> adjacentRockets = FindAdjacentRockets(board, coord);

            if (adjacentRockets.Count > 0)
            {
                return new TapResult
                {
                    Action = TapAction.RocketCombo,
                    TappedCoord = coord,
                    AdjacentRockets = adjacentRockets
                };
            }

            return new TapResult
            {
                Action = TapAction.ExplodeRocket,
                TappedCoord = coord
            };
        }

        // --- Cube: check for valid match group ---
        if (item.IsCube)
        {
            List<Coordinate> matches = matchStrategy.FindMatches(board, coord);

            if (matches.Count >= 2)
            {
                return new TapResult
                {
                    Action = TapAction.BlastGroup,
                    TappedCoord = coord,
                    MatchedCoordinates = matches
                };
            }
        }

        return TapResult.NoAction();
    }

    /// <summary>
    /// Find all rockets in the 4-directional neighbors of a coordinate.
    /// </summary>
    private List<Coordinate> FindAdjacentRockets(Board board, Coordinate coord)
    {
        List<Coordinate> rockets = new List<Coordinate>();

        Coordinate[] neighbors =
        {
            new Coordinate(coord.x, coord.y + 1),
            new Coordinate(coord.x, coord.y - 1),
            new Coordinate(coord.x + 1, coord.y),
            new Coordinate(coord.x - 1, coord.y)
        };

        foreach (var neighbor in neighbors)
        {
            if (!board.IsValidCoordinate(neighbor.x, neighbor.y))
                continue;

            GridItem neighborItem = board.GetItem(neighbor);
            if (neighborItem.IsRocket)
                rockets.Add(neighbor);
        }

        return rockets;
    }
}

/// <summary>
/// Result of a tap resolution. Contains the action type and
/// any pre-computed data the Orchestrator needs to proceed.
/// </summary>
public class TapResult
{
    public TapAction Action;
    public Coordinate TappedCoord;

    // For BlastGroup: the matched coordinates
    public List<Coordinate> MatchedCoordinates;

    // For RocketCombo: adjacent rockets involved
    public List<Coordinate> AdjacentRockets;

    public static TapResult NoAction() => new TapResult
    {
        Action = TapAction.None,
        MatchedCoordinates = new List<Coordinate>(),
        AdjacentRockets = new List<Coordinate>()
    };
}
