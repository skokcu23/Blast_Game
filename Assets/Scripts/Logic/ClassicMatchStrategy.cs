using System.Collections.Generic;
using UnityEngine;

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

        // Optional: Prevent matching empty spaces
        if (gemType == Gem.NONE)
            return new List<Coordinate>();

        HashSet<Coordinate> resultList = new HashSet<Coordinate>();

        // A boolean array is cleaner for true/false "visited" states
        bool[,] visited = new bool[board.Width, board.Height];

        // Start the recursive search
        Dfs(board, resultList, tap, gemType, visited);

        // Convert the HashSet to a List to match your return type
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
        // 1. BOUNDARY CHECK: Stop if we are outside the board
        if (current.x < 0 || current.x >= board.Width || current.y < 0 || current.y >= board.Height)
        {
            return;
        }

        // 2. VISITED CHECK: Stop if we have already checked this tile
        if (visited[current.x, current.y])
        {
            return;
        }

        // 3. Mark the current tile as visited so we don't get stuck in an infinite loop
        visited[current.x, current.y] = true;

        // 4. GEM CHECK: Stop if the gem isn't the color we are looking for
        if (board.GetGem(current) != targetGem)
        {
            return;
        }

        // 5. SUCCESS! It's a match. Add it to our HashSet
        list.Add(current);

        // 6. Recursively check all 4 neighbors (Up, Down, Right, Left)
        Dfs(board, list, new Coordinate(current.x, current.y + 1), targetGem, visited); // Up
        Dfs(board, list, new Coordinate(current.x, current.y - 1), targetGem, visited); // Down
        Dfs(board, list, new Coordinate(current.x + 1, current.y), targetGem, visited); // Right
        Dfs(board, list, new Coordinate(current.x - 1, current.y), targetGem, visited); // Left
    }
}
