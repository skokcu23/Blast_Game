using System.Collections.Generic;

/// <summary>
/// Standard Match-2 blast rules as defined in the Case Study:
/// - At least 2 adjacent same-color cubes to blast
/// - Group >= 4 triggers Rocket creation
/// - Damage to adjacent obstacles is delegated to DamageResolver
///
/// This class owns: match-finding and blast execution.
/// This class does NOT own: damage rules (that's DamageResolver's job).
/// </summary>
public class ClassicMatchStrategy : MatchStrategy
{
    public List<Coordinate> FindMatches(Board board, Coordinate tap)
    {
        GridItem tappedItem = board.GetItem(tap);

        // Only color cubes can be tapped to start a match
        if (!tappedItem.IsCube)
            return new List<Coordinate>();

        HashSet<Coordinate> resultSet = new HashSet<Coordinate>();
        bool[,] visited = new bool[board.Width, board.Height];

        FloodFill(board, resultSet, tap, tappedItem.Id, visited);

        // REQUIREMENT: At least 2 adjacent cubes to blast
        if (resultSet.Count < 2)
            return new List<Coordinate>();

        return new List<Coordinate>(resultSet);
    }

    public BlastResult Blast(Board board, List<Coordinate> matches, Coordinate tappedCell)
    {
        BlastResult result = new BlastResult
        {
            IsValid = true,
            BlastedCoordinates = matches,
            ShouldCreateRocket = matches.Count >= 4,
            RocketSpawnPosition = tappedCell
        };

        // 1. Clear all matched cubes
        foreach (var coord in matches)
        {
            board.SetItem(coord, ItemFactory.CreateEmpty());
        }

        // 2. Delegate adjacent damage to DamageResolver
        //    This single call handles Box, Stone immunity, Vase cap — everything.
        DamageResolver.ProcessAdjacentDamage(board, matches, DamageSource.Blast, result);

        return result;
    }

    // --- Private ---

    private void FloodFill(
        Board board,
        HashSet<Coordinate> found,
        Coordinate current,
        string targetId,
        bool[,] visited)
    {
        if (!board.IsValidCoordinate(current.x, current.y))
            return;
        if (visited[current.x, current.y])
            return;

        GridItem item = board.GetItem(current);

        if (item.Id != targetId)
            return;

        visited[current.x, current.y] = true;
        found.Add(current);

        FloodFill(board, found, new Coordinate(current.x, current.y + 1), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x, current.y - 1), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x + 1, current.y), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x - 1, current.y), targetId, visited);
    }
}
