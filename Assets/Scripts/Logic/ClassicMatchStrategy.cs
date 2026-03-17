using System.Collections.Generic;

public class ClassicMatchStrategy : MatchStrategy
{
    public void Blast(Board board, List<Coordinate> matches)
    {
        foreach (var coordinate in matches)
        {
            board.SetItem(coordinate, new GridItem(Gem.NONE, false, false, 0));
        }
    }

    public List<Coordinate> findMatches(Board board, Coordinate tap)
    {
        GridItem tappedItem = board.GetItem(tap);

        // Don't try to match empty spaces or obstacles
        if (tappedItem.IsEmpty || tappedItem.IsObstacle)
            return new List<Coordinate>();

        HashSet<Coordinate> resultList = new HashSet<Coordinate>();
        bool[,] visited = new bool[board.Width, board.Height];

        Dfs(board, resultList, tap, tappedItem.GemType, visited);

        // REQUIREMENT: At least 2 adjacent cubes to blast
        if (resultList.Count < 2)
        {
            return new List<Coordinate>(); // Return empty if no valid match
        }

        return new List<Coordinate>(resultList);
    }

    private void Dfs(
        Board board,
        HashSet<Coordinate> list,
        Coordinate current,
        Gem tarGetItem,
        bool[,] visited
    )
    {
        if (current.x < 0 || current.x >= board.Width || current.y < 0 || current.y >= board.Height)
            return;
        if (visited[current.x, current.y])
            return;

        GridItem item = board.GetItem(current);

        if (item.GemType != tarGetItem || item.IsObstacle)
            return;

        visited[current.x, current.y] = true;
        list.Add(current);

        Dfs(board, list, new Coordinate(current.x, current.y + 1), tarGetItem, visited);
        Dfs(board, list, new Coordinate(current.x, current.y - 1), tarGetItem, visited);
        Dfs(board, list, new Coordinate(current.x + 1, current.y), tarGetItem, visited);
        Dfs(board, list, new Coordinate(current.x - 1, current.y), tarGetItem, visited);
    }
}
