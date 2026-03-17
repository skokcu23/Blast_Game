using System.Collections.Generic;

public class ClassicMatchStrategy : MatchStrategy
{
    public void Blast(Board board, List<Coordinate> matches)
    {
        foreach (var coordinate in matches)
        {
            board.SetGem(coordinate, Gem.NONE);
        }
    }

    public List<Coordinate> findMatches(Board board, Coordinate tap)
    {
        Gem gemType = board.GetGem(tap);

        if (gemType == Gem.NONE)
            return new List<Coordinate>();

        HashSet<Coordinate> resultList = new HashSet<Coordinate>();
        bool[,] visited = new bool[board.Width, board.Height];

        Dfs(board, resultList, tap, gemType, visited);

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
        Gem targetGem,
        bool[,] visited
    )
    {
        if (current.x < 0 || current.x >= board.Width || current.y < 0 || current.y >= board.Height)
            return;
        if (visited[current.x, current.y])
            return;
        if (board.GetGem(current) != targetGem)
            return;

        visited[current.x, current.y] = true;
        list.Add(current);

        Dfs(board, list, new Coordinate(current.x, current.y + 1), targetGem, visited);
        Dfs(board, list, new Coordinate(current.x, current.y - 1), targetGem, visited);
        Dfs(board, list, new Coordinate(current.x + 1, current.y), targetGem, visited);
        Dfs(board, list, new Coordinate(current.x - 1, current.y), targetGem, visited);
    }
}
