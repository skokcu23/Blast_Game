using System.Collections.Generic;
using NUnit.Framework;

public class ClassicMatchStrategyTests
{
    private Board _testBoard;
    private ClassicMatchStrategy _strategy;

    [SetUp]
    public void Setup()
    {
        _testBoard = new Board(3, 3);

        // CRITICAL FIX: Initialize every spot to a blank GridItem so DFS doesn't hit 'null'
        for (int x = 0; x < _testBoard.Width; x++)
        {
            for (int y = 0; y < _testBoard.Height; y++)
            {
                _testBoard.SetItem(x, y, new GridItem(Gem.NONE));
            }
        }

        _strategy = new ClassicMatchStrategy();
    }

    [Test]
    public void FindMatches_SingleIsolatedGem_ReturnsEmptyList()
    {
        // Arrange: Make the whole board BLUE
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                _testBoard.SetItem(x, y, new GridItem(Gem.BLUE));
            }
        }

        // Set an isolated RED gem
        _testBoard.SetItem(1, 1, new GridItem(Gem.RED));

        // Act
        var matches = _strategy.findMatches(_testBoard, new Coordinate(1, 1));

        // Assert
        Assert.AreEqual(
            0,
            matches.Count,
            "Clicking a single isolated gem should return an empty list due to the Match-2 rule."
        );
    }

    [Test]
    public void FindMatches_LShapedMatch_FindsAllConnected()
    {
        // Arrange: Create an L-shape of GREEN gems.
        // (The rest of the board is already safely initialized to NONE in Setup)
        _testBoard.SetItem(0, 2, new GridItem(Gem.GREEN));
        _testBoard.SetItem(0, 1, new GridItem(Gem.GREEN));
        _testBoard.SetItem(0, 0, new GridItem(Gem.GREEN));
        _testBoard.SetItem(1, 0, new GridItem(Gem.GREEN));
        _testBoard.SetItem(2, 0, new GridItem(Gem.GREEN));

        // Act
        var matches = _strategy.findMatches(_testBoard, new Coordinate(0, 0));

        // Assert
        Assert.AreEqual(5, matches.Count, "Should find all 5 connected gems in the L-shape.");
    }

    [Test]
    public void Blast_RemovesGemsFromBoard()
    {
        // Arrange
        _testBoard.SetItem(0, 0, new GridItem(Gem.YELLOW));
        List<Coordinate> toBlast = new List<Coordinate> { new Coordinate(0, 0) };

        // Act
        _strategy.Blast(_testBoard, toBlast);

        // Assert
        Assert.AreEqual(
            Gem.NONE,
            _testBoard.GetItem(0, 0).GemType,
            "Blasted coordinate should be emptied."
        );
    }
}
