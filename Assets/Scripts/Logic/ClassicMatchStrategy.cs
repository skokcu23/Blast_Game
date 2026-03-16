using System.Collections.Generic;
using Unity.VisualScripting;
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

        List<Coordinate> resultList = new List<Coordinate>();

        ScanBoard(board, resultList, tap, gemType);

        return resultList;
    }

    private void ScanBoard(Board board, List<Coordinate> list, Coordinate tap, Gem gemType)
    {
        //scan upward
        for (int y = tap.y - 1; y >= 0; y--)
        {
            if (board.GetGem(tap.x, y) == gemType)
            {
                list.Add(new Coordinate(tap.x, y));
            }
        }
        //scan downward
        for (int y = tap.y + 1; y < board.Width; y++)
        {
            if (board.GetGem(tap.x, y) == gemType)
            {
                list.Add(new Coordinate(tap.x, y));
            }
        }

        //scan to left
        for (int x = tap.x - 1; x >= 0; x--)
        {
            if (board.GetGem(x, tap.y) == gemType)
            {
                list.Add(new Coordinate(x, tap.y));
            }
        }
        //scan right
        for (int x = tap.x + 1; x < board.Height; x++)
        {
            if (board.GetGem(x, tap.y) == gemType)
            {
                list.Add(new Coordinate(x, tap.y));
            }
        }
    }
}
