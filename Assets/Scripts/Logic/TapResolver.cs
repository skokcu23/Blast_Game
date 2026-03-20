using System.Collections.Generic;

/// <summary>
/// Determines what action a player tap triggers.
/// Pure C# — no Unity dependency.
///
/// Priority order (first match wins):
///   1. Rocket with adjacent rocket → RocketCombo
///   2. Rocket alone               → ExplodeRocket
///   3. Cube with match group ≥ 2  → BlastGroup
///   4. Everything else            → None
/// </summary>
public class TapResolver
{
    /// <summary>
    /// Resolve a tap at the given coordinate into a TapResult.
    /// The result contains the action type and any pre-computed data
    /// (matched coordinates, adjacent rockets) needed by GameSession.
    /// </summary>
    public TapResult Resolve(Board board, Coordinate coord, MatchStrategy matchStrategy)
    {
        GridItem item = board.GetItem(coord);

        if (item.IsEmpty || item.IsObstacle)
            return TapResult.NoAction();

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

            if (board.GetItem(neighbor).IsRocket)
                rockets.Add(neighbor);
        }

        return rockets;
    }
}

/// <summary>
/// Result of a tap resolution. Contains the action type and
/// any pre-computed data GameSession needs to execute the action.
/// </summary>
public class TapResult
{
    /// <summary>What action this tap triggers.</summary>
    public TapAction Action;

    /// <summary>The coordinate that was tapped.</summary>
    public Coordinate TappedCoord;

    /// <summary>For BlastGroup: all coordinates in the matched group.</summary>
    public List<Coordinate> MatchedCoordinates;

    /// <summary>For RocketCombo: adjacent rocket coordinates involved in the combo.</summary>
    public List<Coordinate> AdjacentRockets;

    /// <summary>Create a result representing no valid action.</summary>
    public static TapResult NoAction() => new TapResult
    {
        Action = TapAction.None,
        MatchedCoordinates = new List<Coordinate>(),
        AdjacentRockets = new List<Coordinate>()
    };
}
