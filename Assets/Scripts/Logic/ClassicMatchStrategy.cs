using System.Collections.Generic;

/// <summary>
/// Standard Match-2 blast rules as defined in the Case Study:
/// - At least 2 adjacent same-color cubes to blast
/// - Adjacent obstacles (Box, Vase) take damage from blasts
/// - Stone only takes damage from Rockets (not blasts)
/// - Vase takes max 1 damage per blast group
/// - Group >= 4 triggers Rocket creation
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

        // 2. Deal adjacent damage to obstacles
        //    We track which obstacles we've already damaged this blast
        //    (Vase rule: max 1 damage per blast group)
        HashSet<Coordinate> alreadyDamaged = new HashSet<Coordinate>();

        foreach (var coord in matches)
        {
            DamageAdjacentObstacles(board, coord, alreadyDamaged, result);
        }

        return result;
    }

    // --- Private helpers ---

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

        // Must be the same color cube
        if (item.Id != targetId)
            return;

        visited[current.x, current.y] = true;
        found.Add(current);

        // 4-directional neighbors
        FloodFill(board, found, new Coordinate(current.x, current.y + 1), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x, current.y - 1), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x + 1, current.y), targetId, visited);
        FloodFill(board, found, new Coordinate(current.x - 1, current.y), targetId, visited);
    }

    private void DamageAdjacentObstacles(
        Board board,
        Coordinate blastCell,
        HashSet<Coordinate> alreadyDamaged,
        BlastResult result)
    {
        Coordinate[] neighbors =
        {
            new Coordinate(blastCell.x, blastCell.y + 1),
            new Coordinate(blastCell.x, blastCell.y - 1),
            new Coordinate(blastCell.x + 1, blastCell.y),
            new Coordinate(blastCell.x - 1, blastCell.y)
        };

        foreach (var neighbor in neighbors)
        {
            if (!board.IsValidCoordinate(neighbor.x, neighbor.y))
                continue;
            if (alreadyDamaged.Contains(neighbor))
                continue;

            GridItem item = board.GetItem(neighbor);

            if (!item.IsObstacle || !item.IsAlive)
                continue;

            // Stone is immune to blast damage (only rockets can hurt it)
            if (item.Id == ItemIds.Stone)
                continue;

            // Deal damage
            alreadyDamaged.Add(neighbor);
            bool destroyed = item.TakeDamage(1);

            if (destroyed)
            {
                board.SetItem(neighbor, ItemFactory.CreateEmpty());
                result.DestroyedObstacles.Add(neighbor);
            }
            else
            {
                result.DamagedObstacles.Add(neighbor);
            }
        }
    }
}
