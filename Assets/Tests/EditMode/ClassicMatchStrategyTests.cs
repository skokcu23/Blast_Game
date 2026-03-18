using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class ClassicMatchStrategyTests
{
    private Board _board;
    private ClassicMatchStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
        _strategy = new ClassicMatchStrategy();
    }

    // --- Helper: place a cube at a coordinate ---
    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // MATCH FINDING
    // ==========================================

    [Test]
    public void FindMatches_TwoAdjacentSameColor_ReturnsTwo()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));

        Assert.AreEqual(2, matches.Count);
        CollectionAssert.Contains(matches, new Coordinate(0, 0));
        CollectionAssert.Contains(matches, new Coordinate(1, 0));
    }

    [Test]
    public void FindMatches_SingleCube_ReturnsEmpty()
    {
        PlaceCube(3, 3, ItemIds.Blue);

        var matches = _strategy.FindMatches(_board, new Coordinate(3, 3));

        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_TwoDifferentColors_ReturnsEmpty()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Blue);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));

        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_LShapeCluster_ReturnsAll()
    {
        // L-shape:
        // R .
        // R .
        // R R
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));

        Assert.AreEqual(4, matches.Count);
    }

    [Test]
    public void FindMatches_DiagonalNotConnected()
    {
        // Diagonal should NOT connect:
        // . R
        // R .
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 1, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));

        Assert.AreEqual(0, matches.Count); // Only 1 in group, need 2
    }

    [Test]
    public void FindMatches_LargeConnectedGroup()
    {
        // 5 connected cubes in a plus shape
        //   G
        // G G G
        //   G
        PlaceCube(2, 2, ItemIds.Green);
        PlaceCube(1, 2, ItemIds.Green);
        PlaceCube(3, 2, ItemIds.Green);
        PlaceCube(2, 1, ItemIds.Green);
        PlaceCube(2, 3, ItemIds.Green);

        var matches = _strategy.FindMatches(_board, new Coordinate(2, 2));

        Assert.AreEqual(5, matches.Count);
    }

    [Test]
    public void FindMatches_TappingEmptyCell_ReturnsEmpty()
    {
        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_TappingObstacle_ReturnsEmpty()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Box));

        var matches = _strategy.FindMatches(_board, new Coordinate(3, 3));

        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_ColorsDontCrossThroughObstacle()
    {
        // R [BOX] R — the two reds are NOT connected
        PlaceCube(0, 0, ItemIds.Red);
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(2, 0, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));

        Assert.AreEqual(0, matches.Count); // Only 1 red reachable
    }

    // ==========================================
    // BLAST
    // ==========================================

    [Test]
    public void Blast_ClearsMatchedCubes()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        _strategy.Blast(_board, matches, new Coordinate(0, 0));

        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty);
        Assert.IsTrue(_board.GetItem(1, 0).IsEmpty);
    }

    [Test]
    public void Blast_ShouldCreateRocket_WhenGroupGTE4()
    {
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0)
        };

        foreach (var c in matches)
            PlaceCube(c.x, c.y, ItemIds.Blue);

        var result = _strategy.Blast(_board, matches, new Coordinate(1, 0));

        Assert.IsTrue(result.ShouldCreateRocket);
        Assert.AreEqual(new Coordinate(1, 0), result.RocketSpawnPosition);
    }

    [Test]
    public void Blast_ShouldNotCreateRocket_WhenGroupLT4()
    {
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0)
        };

        foreach (var c in matches)
            PlaceCube(c.x, c.y, ItemIds.Blue);

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));

        Assert.IsFalse(result.ShouldCreateRocket);
    }

    // ==========================================
    // OBSTACLE DAMAGE FROM BLAST
    // ==========================================

    [Test]
    public void Blast_DamagesAdjacentBox()
    {
        // Setup: Red cubes at (0,0) and (1,0), Box at (2,0)
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));

        // Box should be destroyed (1 HP)
        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty);
        CollectionAssert.Contains(result.DestroyedObstacles, new Coordinate(2, 0));
    }

    [Test]
    public void Blast_StoneIsImmuneToBlastDamage()
    {
        // Setup: Red cubes at (0,0) and (1,0), Stone at (2,0)
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Stone));

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));

        // Stone should be untouched
        Assert.AreEqual(ItemIds.Stone, _board.GetItem(2, 0).Id);
        Assert.AreEqual(1, _board.GetItem(2, 0).Health);
        Assert.AreEqual(0, result.DamagedObstacles.Count);
        Assert.AreEqual(0, result.DestroyedObstacles.Count);
    }

    [Test]
    public void Blast_VaseTakesOnlyOneDamagePerBlast()
    {
        // Setup: Vase at (1,1) surrounded by red cubes on two sides
        //   R
        // R V
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(1, 2, ItemIds.Red);
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Vase));

        // Also connect the reds so they form a valid group
        PlaceCube(0, 2, ItemIds.Red);

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 1),
            new Coordinate(1, 2),
            new Coordinate(0, 2)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 1));

        // Vase had 2 HP, should now have 1 HP (only 1 damage per blast)
        var vase = _board.GetItem(1, 1);
        Assert.AreEqual(1, vase.Health);
        Assert.IsTrue(vase.IsAlive);
        CollectionAssert.Contains(result.DamagedObstacles, new Coordinate(1, 1));
    }

    [Test]
    public void Blast_VaseDestroyedBySecondBlast()
    {
        // First blast: damage vase to 1 HP
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Vase));
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);

        var matches1 = new List<Coordinate>
        {
            new Coordinate(0, 1),
            new Coordinate(0, 2)
        };
        _strategy.Blast(_board, matches1, new Coordinate(0, 1));

        Assert.AreEqual(1, _board.GetItem(1, 1).Health);

        // Second blast: destroy vase
        PlaceCube(2, 1, ItemIds.Blue);
        PlaceCube(2, 2, ItemIds.Blue);

        var matches2 = new List<Coordinate>
        {
            new Coordinate(2, 1),
            new Coordinate(2, 2)
        };
        var result = _strategy.Blast(_board, matches2, new Coordinate(2, 1));

        Assert.IsTrue(_board.GetItem(1, 1).IsEmpty);
        CollectionAssert.Contains(result.DestroyedObstacles, new Coordinate(1, 1));
    }

    [Test]
    public void Blast_ObstacleNotAdjacentToBlast_Unharmed()
    {
        // Box far away from the blast
        _board.SetItem(7, 7, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        _strategy.Blast(_board, matches, new Coordinate(0, 0));

        Assert.AreEqual(1, _board.GetItem(7, 7).Health);
    }
}
