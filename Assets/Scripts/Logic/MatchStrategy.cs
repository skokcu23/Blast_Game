using System;
using System.Collections.Generic;

interface MatchStrategy
{
    List<Coordinate> findMatches(Board board, Coordinate tap);

    void Blast(Board board, List<Coordinate> matches);
}
