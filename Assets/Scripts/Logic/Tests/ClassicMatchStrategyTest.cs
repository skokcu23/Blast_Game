using System.Collections.Generic;
using NUnit.Framework;

public class ClassicMatchStrategyTests
{
    private Board _testBoard;
    private ClassicMatchStrategy _strategy;

    [SetUp]
    public void Setup()
    {
        // This runs before EVERY test, giving us a fresh 3x3 board
        _testBoard = new Board(3, 3);
        _strategy = new ClassicMatchStrategy();
    }

    [Test]
    public void FindMatches_SingleIsolatedGem_ReturnsEmptyList()
    {
        // Arrange: Make the center RED, and everything else BLUE
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                _testBoard.SetGem(x, y, Gem.BLUE);
            }
        }

        _testBoard.SetGem(1, 1, Gem.RED);

        // Act: Click the isolated RED gem
        var matches = _strategy.findMatches(_testBoard, new Coordinate(1, 1));

        // Assert: Because of the Match-2 rule, an isolated gem should return 0 matches
        Assert.AreEqual(
            0,
            matches.Count,
            "Clicking a single isolated gem should return an empty list due to the Match-2 rule."
        );
    }

    [Test]
    public void FindMatches_LShapedMatch_FindsAllConnected()
    {
        // Arrange: Create an L-shape of GREEN gems
        /*
         * [G][ ][ ]
         * [G][ ][ ]
         * [G][G][G]
         */
        _testBoard.SetGem(0, 2, Gem.GREEN);
        _testBoard.SetGem(0, 1, Gem.GREEN);
        _testBoard.SetGem(0, 0, Gem.GREEN);
        _testBoard.SetGem(1, 0, Gem.GREEN);
        _testBoard.SetGem(2, 0, Gem.GREEN);

        // Act: Click the corner
        var matches = _strategy.findMatches(_testBoard, new Coordinate(0, 0));

        // Assert
        Assert.AreEqual(5, matches.Count, "Should find all 5 connected gems in the L-shape.");
    }

    [Test]
    public void Blast_RemovesGemsFromBoard()
    {
        // Arrange
        _testBoard.SetGem(0, 0, Gem.YELLOW);
        List<Coordinate> toBlast = new List<Coordinate> { new Coordinate(0, 0) };

        // Act
        _strategy.Blast(_testBoard, toBlast);

        // Assert
        // Assuming your Blast method sets the gem to a 'NONE' or 'EMPTY' state.
        // Adjust Gem.NONE to whatever your empty state enum is!
        Assert.AreEqual(Gem.NONE, _testBoard.GetGem(0, 0), "Blasted coordinate should be emptied.");
    }
}
