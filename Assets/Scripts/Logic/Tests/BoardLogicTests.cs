using NUnit.Framework;

public class BoardLogicTests
{
    [Test]
    public void ClassicMatchStrategy_FindsValidMatchOfThree()
    {
        // ARRANGE: Set up a tiny controlled board (3x3)
        Board testBoard = new Board(3, 3);

        // CRITICAL FIX: Fill empty spaces to prevent null references
        for (int x = 0; x < testBoard.Width; x++)
        {
            for (int y = 0; y < testBoard.Height; y++)
            {
                testBoard.SetItem(x, y, new GridItem(Gem.NONE));
            }
        }

        // Force the bottom row to all be RED
        testBoard.SetItem(0, 0, new GridItem(Gem.RED));
        testBoard.SetItem(1, 0, new GridItem(Gem.RED));
        testBoard.SetItem(2, 0, new GridItem(Gem.RED));

        ClassicMatchStrategy strategy = new ClassicMatchStrategy();
        Coordinate clickTarget = new Coordinate(1, 0); // Click the middle red gem

        // ACT: Run the logic
        var matches = strategy.findMatches(testBoard, clickTarget);

        // ASSERT: Verify the logic returns exactly 3 matches
        Assert.AreEqual(
            3,
            matches.Count,
            "The algorithm should have found exactly 3 matching gems."
        );
        Assert.IsTrue(matches.Contains(new Coordinate(0, 0)));
        Assert.IsTrue(matches.Contains(new Coordinate(1, 0)));
        Assert.IsTrue(matches.Contains(new Coordinate(2, 0)));
    }
}
