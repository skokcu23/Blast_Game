using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// Updated for Iteration 2: Damage logic is now in DamageResolver.
/// These tests focus on what ClassicMatchStrategy owns: match finding and blast execution.
/// Obstacle damage integration is verified end-to-end here but tested in depth in DamageResolverTests.
/// </summary>
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

    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // MATCH FINDING (unchanged from Iteration 1)
    // ==========================================

    [Test]
    public void FindMatches_TwoAdjacentSameColor_ReturnsTwo()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(2, matches.Count);
    }

    [Test]
    public void FindMatches_SingleCube_ReturnsEmpty()
    {
        PlaceCube(3, 3, ItemIds.Blue);
        var matches = _strategy.FindMatches(_board, new Coordinate(3, 3));
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_DiagonalNotConnected()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 1, ItemIds.Red);
        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_LShapeCluster_ReturnsAll()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);
        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(4, matches.Count);
    }

    [Test]
    public void FindMatches_TappingObstacle_ReturnsEmpty()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Box));
        var matches = _strategy.FindMatches(_board, new Coordinate(3, 3));
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_TappingEmpty_ReturnsEmpty()
    {
        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(0, matches.Count);
    }

    [Test]
    public void FindMatches_ObstacleBlocksFloodFill()
    {
        PlaceCube(0, 0, ItemIds.Red);
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(2, 0, ItemIds.Red);

        var matches = _strategy.FindMatches(_board, new Coordinate(0, 0));
        Assert.AreEqual(0, matches.Count);
    }

    // ==========================================
    // BLAST EXECUTION
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
    public void Blast_RocketFlag_TrueWhenGTE4()
    {
        var matches = new List<Coordinate>();
        for (int i = 0; i < 4; i++)
        {
            PlaceCube(i, 0, ItemIds.Blue);
            matches.Add(new Coordinate(i, 0));
        }

        var result = _strategy.Blast(_board, matches, new Coordinate(1, 0));

        Assert.IsTrue(result.ShouldCreateRocket);
        Assert.AreEqual(new Coordinate(1, 0), result.RocketSpawnPosition);
    }

    [Test]
    public void Blast_RocketFlag_FalseWhenLT4()
    {
        var matches = new List<Coordinate>();
        for (int i = 0; i < 3; i++)
        {
            PlaceCube(i, 0, ItemIds.Blue);
            matches.Add(new Coordinate(i, 0));
        }

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));
        Assert.IsFalse(result.ShouldCreateRocket);
    }

    // ==========================================
    // BLAST + DAMAGE INTEGRATION
    // (verifies ClassicMatchStrategy correctly delegates to DamageResolver)
    // ==========================================

    [Test]
    public void Blast_AdjacentBox_Destroyed()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));

        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty);
        Assert.AreEqual(1, result.DestroyedObstacles.Count);
        Assert.AreEqual(1, result.DestroyedObstacleInfos.Count);
        Assert.AreEqual(ItemIds.Box, result.DestroyedObstacleInfos[0].ObstacleId);
    }

    [Test]
    public void Blast_AdjacentStone_Immune()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Stone));

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 0));

        Assert.AreEqual(ItemIds.Stone, _board.GetItem(2, 0).Id);
        Assert.AreEqual(1, _board.GetItem(2, 0).Health);
        Assert.AreEqual(0, result.DamagedObstacles.Count);
    }

    [Test]
    public void Blast_AdjacentVase_OneDamageOnly()
    {
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Vase));
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(1, 2, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Red);

        var matches = new List<Coordinate>
        {
            new Coordinate(0, 1),
            new Coordinate(1, 2),
            new Coordinate(0, 2)
        };

        var result = _strategy.Blast(_board, matches, new Coordinate(0, 1));

        Assert.AreEqual(1, _board.GetItem(1, 1).Health);
        Assert.AreEqual(1, result.DamagedObstacles.Count);
    }
}
