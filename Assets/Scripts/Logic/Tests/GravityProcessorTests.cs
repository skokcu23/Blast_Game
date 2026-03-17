using System.Collections.Generic;
using NUnit.Framework;

public class GravityProcessorTests
{
    private GravityProcessor _processor;

    [SetUp]
    public void Setup()
    {
        _processor = new GravityProcessor();
    }

    // Helper method to guarantee no null references
    private Board CreateSafeBoard(int width, int height)
    {
        Board board = new Board(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                board.SetItem(x, y, new GridItem(Gem.NONE));
            }
        }
        return board;
    }

    [Test]
    public void ApplyGravity_WithEmptySpaceAtBottom_GemsFallDown()
    {
        // ARRANGE
        Board board = CreateSafeBoard(1, 3);
        board.SetItem(0, 0, new GridItem(Gem.NONE)); // Bottom is empty
        board.SetItem(0, 1, new GridItem(Gem.RED)); // Middle is Red
        board.SetItem(0, 2, new GridItem(Gem.BLUE)); // Top is Blue

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT
        Assert.AreEqual(Gem.RED, board.GetItem(0, 0).GemType, "Red gem should fall to the bottom.");
        Assert.AreEqual(
            Gem.BLUE,
            board.GetItem(0, 1).GemType,
            "Blue gem should fall to the middle."
        );
        Assert.AreEqual(Gem.NONE, board.GetItem(0, 2).GemType, "Top spot should now be empty.");

        Assert.AreEqual(2, movements.Count, "Exactly 2 gems should have moved.");
        Assert.AreEqual(new Coordinate(0, 1), movements[0].StartPos);
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
    }

    [Test]
    public void ApplyGravity_WithMultipleEmptySpaces_GemFallsMultipleRows()
    {
        // ARRANGE
        Board board = CreateSafeBoard(1, 3);
        board.SetItem(0, 0, new GridItem(Gem.NONE));
        board.SetItem(0, 1, new GridItem(Gem.NONE));
        board.SetItem(0, 2, new GridItem(Gem.GREEN));

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT
        Assert.AreEqual(
            Gem.GREEN,
            board.GetItem(0, 0).GemType,
            "Green gem should drop all the way to the bottom."
        );
        Assert.AreEqual(1, movements.Count, "Only 1 gem moved.");
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
    }

    [Test]
    public void ApplyGravity_NoEmptySpaces_DoesNothing()
    {
        // ARRANGE
        Board board = CreateSafeBoard(1, 2);
        board.SetItem(0, 0, new GridItem(Gem.RED));
        board.SetItem(0, 1, new GridItem(Gem.BLUE));

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT
        Assert.AreEqual(0, movements.Count, "No movements should be recorded.");
        Assert.AreEqual(Gem.RED, board.GetItem(0, 0).GemType, "Red should still be at bottom.");
    }

    [Test]
    public void FillEmptySpaces_ReplacesAllNoneGemsWithNewColors()
    {
        // ARRANGE
        Board board = CreateSafeBoard(2, 2);
        board.SetItem(0, 0, new GridItem(Gem.RED));
        board.SetItem(0, 1, new GridItem(Gem.NONE));
        board.SetItem(1, 0, new GridItem(Gem.NONE));
        board.SetItem(1, 1, new GridItem(Gem.BLUE));

        // ACT
        List<GemMovement> newGems = _processor.FillEmptySpaces(board);

        // ASSERT
        Assert.AreEqual(2, newGems.Count, "Should generate exactly 2 new gems.");
        Assert.AreNotEqual(
            Gem.NONE,
            board.GetItem(0, 1).GemType,
            "Spot (0,1) should have a new color."
        );
        Assert.AreNotEqual(
            Gem.NONE,
            board.GetItem(1, 0).GemType,
            "Spot (1,0) should have a new color."
        );
        Assert.AreEqual(
            3,
            newGems[0].StartPos.y,
            "The gem falling to y=1 should spawn at height 3."
        );
        Assert.AreEqual(
            2,
            newGems[1].StartPos.y,
            "The gem falling to y=0 should spawn at height 2."
        );
    }
}
