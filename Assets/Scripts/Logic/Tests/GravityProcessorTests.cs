using System.Collections.Generic;
using NUnit.Framework;

public class GravityProcessorTests
{
    private GravityProcessor _processor;

    [SetUp]
    public void Setup()
    {
        // This runs before every test to give us a fresh processor
        _processor = new GravityProcessor();
    }

    [Test]
    public void ApplyGravity_WithEmptySpaceAtBottom_GemsFallDown()
    {
        // ARRANGE: A single column, 3 rows high.
        Board board = new Board(1, 3);
        board.SetGem(0, 0, Gem.NONE); // Bottom is empty!
        board.SetGem(0, 1, Gem.RED); // Middle is Red
        board.SetGem(0, 2, Gem.BLUE); // Top is Blue

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT: Logic Board checks
        Assert.AreEqual(Gem.RED, board.GetGem(0, 0), "Red gem should fall to the bottom.");
        Assert.AreEqual(Gem.BLUE, board.GetGem(0, 1), "Blue gem should fall to the middle.");
        Assert.AreEqual(Gem.NONE, board.GetGem(0, 2), "Top spot should now be empty.");

        // ASSERT: Movement Data checks
        Assert.AreEqual(2, movements.Count, "Exactly 2 gems should have moved.");
        Assert.AreEqual(new Coordinate(0, 1), movements[0].StartPos);
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
    }

    [Test]
    public void ApplyGravity_WithMultipleEmptySpaces_GemFallsMultipleRows()
    {
        // ARRANGE
        Board board = new Board(1, 3);
        board.SetGem(0, 0, Gem.NONE);
        board.SetGem(0, 1, Gem.NONE);
        board.SetGem(0, 2, Gem.GREEN); // Top is Green, bottom two are empty

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT
        Assert.AreEqual(
            Gem.GREEN,
            board.GetGem(0, 0),
            "Green gem should drop all the way to the bottom."
        );
        Assert.AreEqual(1, movements.Count, "Only 1 gem moved.");
        Assert.AreEqual(
            new Coordinate(0, 0),
            movements[0].EndPos,
            "The movement end position should be row 0."
        );
    }

    [Test]
    public void ApplyGravity_NoEmptySpaces_DoesNothing()
    {
        // ARRANGE: A fully packed board
        Board board = new Board(1, 2);
        board.SetGem(0, 0, Gem.RED);
        board.SetGem(0, 1, Gem.BLUE);

        // ACT
        List<GemMovement> movements = _processor.ApplyGravity(board);

        // ASSERT
        Assert.AreEqual(0, movements.Count, "No movements should be recorded.");
        Assert.AreEqual(Gem.RED, board.GetGem(0, 0), "Red should still be at bottom.");
    }

    [Test]
    public void FillEmptySpaces_ReplacesAllNoneGemsWithNewColors()
    {
        // ARRANGE
        Board board = new Board(2, 2);
        board.SetGem(0, 0, Gem.RED);
        board.SetGem(0, 1, Gem.NONE); // Needs refill (y = 1)
        board.SetGem(1, 0, Gem.NONE); // Needs refill (y = 0)
        board.SetGem(1, 1, Gem.BLUE);

        // ACT
        List<GemMovement> newGems = _processor.FillEmptySpaces(board);

        // ASSERT
        Assert.AreEqual(2, newGems.Count, "Should generate exactly 2 new gems.");

        // Ensure the board memory was updated
        Assert.AreNotEqual(Gem.NONE, board.GetGem(0, 1), "Spot (0,1) should have a new color.");
        Assert.AreNotEqual(Gem.NONE, board.GetGem(1, 0), "Spot (1,0) should have a new color.");

        // newGems[0] is for space (0,1). Math: Height(2) + y(1) = 3
        Assert.AreEqual(
            3,
            newGems[0].StartPos.y,
            "The gem falling to y=1 should spawn at height 3."
        );

        // newGems[1] is for space (1,0). Math: Height(2) + y(0) = 2
        Assert.AreEqual(
            2,
            newGems[1].StartPos.y,
            "The gem falling to y=0 should spawn at height 2."
        );
    }
}
