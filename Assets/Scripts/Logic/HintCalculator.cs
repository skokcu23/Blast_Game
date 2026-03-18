using System.Collections.Generic;

/// <summary>
/// Scans the board and identifies all cube groups with ≥ 4 members.
/// These groups should display a rocket hint icon on each cube.
///
/// Pure C# — no Unity dependency. Called by Orchestrator after every
/// board state change (blast → gravity → refill → recalculate hints).
///
/// Case Study rule: "Cube groups should display a rocket logo on them
/// if they are eligible to create one."
/// </summary>
public static class HintCalculator
{
    /// <summary>
    /// Find all coordinates that belong to a group of ≥ 4 same-color cubes.
    /// Returns a dictionary mapping coordinate → item ID (color) for sprite selection.
    /// </summary>
    public static Dictionary<Coordinate, string> FindRocketHints(Board board)
    {
        Dictionary<Coordinate, string> allHints = new Dictionary<Coordinate, string>();
        bool[,] visited = new bool[board.Width, board.Height];

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (visited[x, y])
                    continue;

                GridItem item = board.GetItem(x, y);

                if (!item.IsCube)
                    continue;

                HashSet<Coordinate> group = new HashSet<Coordinate>();
                FloodFill(board, new Coordinate(x, y), item.Id, visited, group);

                if (group.Count >= 4)
                {
                    foreach (var coord in group)
                        allHints[coord] = item.Id;
                }
            }
        }

        return allHints;
    }

    /// <summary>
    /// Get groups as separate lists (for more granular UI control).
    /// Each inner list is one connected group of ≥ 4 cubes.
    /// </summary>
    public static List<List<Coordinate>> FindRocketHintGroups(Board board)
    {
        List<List<Coordinate>> groups = new List<List<Coordinate>>();
        bool[,] visited = new bool[board.Width, board.Height];

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (visited[x, y])
                    continue;

                GridItem item = board.GetItem(x, y);

                if (!item.IsCube)
                    continue;

                HashSet<Coordinate> group = new HashSet<Coordinate>();
                FloodFill(board, new Coordinate(x, y), item.Id, visited, group);

                if (group.Count >= 4)
                    groups.Add(new List<Coordinate>(group));
            }
        }

        return groups;
    }

    private static void FloodFill(
        Board board,
        Coordinate current,
        string targetId,
        bool[,] visited,
        HashSet<Coordinate> group)
    {
        if (!board.IsValidCoordinate(current.x, current.y))
            return;
        if (visited[current.x, current.y])
            return;

        GridItem item = board.GetItem(current);
        if (item.Id != targetId)
            return;

        visited[current.x, current.y] = true;
        group.Add(current);

        FloodFill(board, new Coordinate(current.x, current.y + 1), targetId, visited, group);
        FloodFill(board, new Coordinate(current.x, current.y - 1), targetId, visited, group);
        FloodFill(board, new Coordinate(current.x + 1, current.y), targetId, visited, group);
        FloodFill(board, new Coordinate(current.x - 1, current.y), targetId, visited, group);
    }
}
