using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class HintCalculatorTests
{
    private Board _board;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
    }

    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // No hints
    // ==========================================

    [Test]
    public void FindRocketHints_EmptyBoard_ReturnsEmpty()
    {
        var hints = HintCalculator.FindRocketHints(_board);
        Assert.AreEqual(0, hints.Count);
    }

    [Test]
    public void FindRocketHints_GroupOf2_NoHints()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var hints = HintCalculator.FindRocketHints(_board);
        Assert.AreEqual(0, hints.Count);
    }

    [Test]
    public void FindRocketHints_GroupOf3_NoHints()
    {
        PlaceCube(0, 0, ItemIds.Blue);
        PlaceCube(1, 0, ItemIds.Blue);
        PlaceCube(2, 0, ItemIds.Blue);

        var hints = HintCalculator.FindRocketHints(_board);
        Assert.AreEqual(0, hints.Count);
    }

    // ==========================================
    // Hints present
    // ==========================================

    [Test]
    public void FindRocketHints_GroupOf4_AllFourHinted()
    {
        PlaceCube(0, 0, ItemIds.Green);
        PlaceCube(1, 0, ItemIds.Green);
        PlaceCube(2, 0, ItemIds.Green);
        PlaceCube(3, 0, ItemIds.Green);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(4, hints.Count);
        Assert.IsTrue(hints.ContainsKey(new Coordinate(0, 0)));
        Assert.IsTrue(hints.ContainsKey(new Coordinate(1, 0)));
        Assert.IsTrue(hints.ContainsKey(new Coordinate(2, 0)));
        Assert.IsTrue(hints.ContainsKey(new Coordinate(3, 0)));
    }

    [Test]
    public void FindRocketHints_GroupOf5_AllFiveHinted()
    {
        // Plus shape
        PlaceCube(2, 2, ItemIds.Yellow);
        PlaceCube(1, 2, ItemIds.Yellow);
        PlaceCube(3, 2, ItemIds.Yellow);
        PlaceCube(2, 1, ItemIds.Yellow);
        PlaceCube(2, 3, ItemIds.Yellow);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(5, hints.Count);
    }

    [Test]
    public void FindRocketHints_LShapeGroupOf4()
    {
        // L shape
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(4, hints.Count);
    }

    // ==========================================
    // Multiple groups
    // ==========================================

    [Test]
    public void FindRocketHints_TwoSeparateGroups_BothHinted()
    {
        // Group 1: 4 reds on left
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);
        PlaceCube(0, 3, ItemIds.Red);

        // Group 2: 4 blues on right
        PlaceCube(7, 0, ItemIds.Blue);
        PlaceCube(7, 1, ItemIds.Blue);
        PlaceCube(7, 2, ItemIds.Blue);
        PlaceCube(7, 3, ItemIds.Blue);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(8, hints.Count);
    }

    [Test]
    public void FindRocketHints_OneGroupOf4_OneGroupOf3_OnlyFirstHinted()
    {
        // Group of 4: hinted
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        PlaceCube(2, 0, ItemIds.Red);
        PlaceCube(3, 0, ItemIds.Red);

        // Group of 3: NOT hinted
        PlaceCube(6, 0, ItemIds.Blue);
        PlaceCube(7, 0, ItemIds.Blue);
        PlaceCube(7, 1, ItemIds.Blue);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(4, hints.Count);
        Assert.IsFalse(hints.ContainsKey(new Coordinate(6, 0)));
        Assert.IsFalse(hints.ContainsKey(new Coordinate(7, 0)));
    }

    // ==========================================
    // Edge cases
    // ==========================================

    [Test]
    public void FindRocketHints_DiagonalCubesSameColor_NotConnected()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 1, ItemIds.Red);
        PlaceCube(2, 2, ItemIds.Red);
        PlaceCube(3, 3, ItemIds.Red);

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(0, hints.Count); // Diagonals don't connect
    }

    [Test]
    public void FindRocketHints_ObstaclesIgnored()
    {
        // 4 cubes with an obstacle mixed in — obstacle breaks the group
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(3, 0, ItemIds.Red);
        PlaceCube(4, 0, ItemIds.Red);

        var hints = HintCalculator.FindRocketHints(_board);

        // Two groups of 2 reds, neither qualifies
        Assert.AreEqual(0, hints.Count);
    }

    [Test]
    public void FindRocketHints_RocketsNotIncluded()
    {
        // Rockets on the board should not appear as hints
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var hints = HintCalculator.FindRocketHints(_board);

        Assert.AreEqual(0, hints.Count);
    }


}
