using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tests for SINGLE rocket explosions and chain reactions.
/// Combo tests are in ComboDamageTests.cs (the single source of truth for combos).
/// </summary>
[TestFixture]
public class RocketExplosionTests
{
    private Board _board;
    private RocketProcessor _processor;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
        _processor = new RocketProcessor(42);
    }

    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // BASIC EXPLOSION
    // ==========================================

    [Test]
    public void ExplodeRocket_NonRocketCell_ReturnsNull()
    {
        PlaceCube(3, 3, ItemIds.Red);
        Assert.IsNull(_processor.ExplodeRocket(_board, new Coordinate(3, 3)));
    }

    [Test]
    public void ExplodeRocket_EmptyCell_ReturnsNull()
    {
        Assert.IsNull(_processor.ExplodeRocket(_board, new Coordinate(0, 0)));
    }

    [Test]
    public void ExplodeRocket_RemovesItsOwnRocketFromBoard()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _processor.ExplodeRocket(_board, new Coordinate(3, 3));
        Assert.IsTrue(_board.GetItem(3, 3).IsEmpty);
    }

    [Test]
    public void ExplodeRocket_HorizontalRocket_TracesLeftAndRight()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(result.IsHorizontal);
        Assert.AreEqual(3, result.PathA.Count); // LEFT: (2,3),(1,3),(0,3)
        Assert.AreEqual(new Coordinate(2, 3), result.PathA[0]);
        Assert.AreEqual(new Coordinate(0, 3), result.PathA[2]);
        Assert.AreEqual(4, result.PathB.Count); // RIGHT: (4,3)...(7,3)
        Assert.AreEqual(new Coordinate(4, 3), result.PathB[0]);
        Assert.AreEqual(new Coordinate(7, 3), result.PathB[3]);
    }

    [Test]
    public void ExplodeRocket_VerticalRocket_TracesDownAndUp()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsFalse(result.IsHorizontal);
        Assert.AreEqual(3, result.PathA.Count); // DOWN
        Assert.AreEqual(4, result.PathB.Count); // UP
    }

    [Test]
    public void ExplodeRocket_AtCorner_ClipsToBoard()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(0, 0));

        Assert.AreEqual(0, result.PathA.Count); // Nothing left
        Assert.AreEqual(7, result.PathB.Count); // 7 cells right
    }

    [Test]
    public void ExplodeRocket_Single_IsComboFalse()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsFalse(result.IsCombo, "Single rocket should not be a combo");
        Assert.IsNull(result.ParallelPathsA);
        Assert.IsNull(result.ParallelPathsB);
    }

    // ==========================================
    // DESTROYING CUBES ALONG PATH
    // ==========================================

    [Test]
    public void ExplodeRocket_DestroysCubesInPath()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        PlaceCube(1, 3, ItemIds.Red);
        PlaceCube(5, 3, ItemIds.Blue);

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(1, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        Assert.AreEqual(2, result.DestroyedCubes.Count);
    }

    [Test]
    public void ExplodeRocket_CubeInDestroyedCubesList()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        PlaceCube(5, 3, ItemIds.Red);

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        CollectionAssert.Contains(result.DestroyedCubes, new Coordinate(5, 3));
    }

    // ==========================================
    // OBSTACLE DAMAGE
    // ==========================================

    [Test]
    public void ExplodeRocket_DestroysBox()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Box));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        CollectionAssert.Contains(result.DestroyedObstacles, new Coordinate(5, 3));
    }

    [Test]
    public void ExplodeRocket_DestroysStone()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Stone));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
    }

    [Test]
    public void ExplodeRocket_DamagesVaseNotDestroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Vase));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.AreEqual(1, _board.GetItem(5, 3).Health);
        CollectionAssert.Contains(result.DamagedObstacles, new Coordinate(5, 3));
    }

    [Test]
    public void ExplodeRocket_PreDamagedVase_Destroyed()
    {
        var vase = ItemFactory.CreateItem(ItemIds.Vase);
        vase.TakeDamage(1); // 2 → 1 HP
        _board.SetItem(5, 3, vase);
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        CollectionAssert.Contains(result.DestroyedObstacles, new Coordinate(5, 3));
    }

    // ==========================================
    // CHAIN REACTIONS — Triggered rockets
    // ==========================================

    [Test]
    public void ExplodeRocket_TriggersAnotherRocket_StaysOnBoard()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        CollectionAssert.Contains(result.TriggeredRockets, new Coordinate(5, 3));
        Assert.IsTrue(_board.GetItem(5, 3).IsRocket, "Triggered rocket must stay on board");
    }

    [Test]
    public void ExplodeRocket_ChainABC()
    {
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(5, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var resultA = _processor.ExplodeRocket(_board, new Coordinate(0, 3));
        CollectionAssert.Contains(resultA.TriggeredRockets, new Coordinate(5, 3));
        Assert.IsTrue(_board.GetItem(5, 3).IsRocket);

        var resultB = _processor.ExplodeRocket(_board, new Coordinate(5, 3));
        CollectionAssert.Contains(resultB.TriggeredRockets, new Coordinate(5, 7));
        Assert.IsTrue(_board.GetItem(5, 7).IsRocket);

        var resultC = _processor.ExplodeRocket(_board, new Coordinate(5, 7));
        Assert.IsNotNull(resultC);

        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(5, 7).IsEmpty);
    }

    [Test]
    public void ExplodeRocket_SameRocketNotTriggeredTwice()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        int count = result.TriggeredRockets.Count(c => c == new Coordinate(5, 3));
        Assert.AreEqual(1, count);
    }

    [Test]
    public void ExplodeRocket_AlreadyExplodedRocket_ReturnsNull()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsNull(_processor.ExplodeRocket(_board, new Coordinate(3, 3)));
    }

    [Test]
    public void ExplodeRocket_HitsMultipleRockets()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(1, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(6, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.AreEqual(2, result.TriggeredRockets.Count);
        Assert.IsTrue(_board.GetItem(1, 3).IsRocket);
        Assert.IsTrue(_board.GetItem(6, 3).IsRocket);
    }

    // ==========================================
    // GRAVITY INTERACTION
    // ==========================================

    [Test]
    public void Rocket_FallsWithGravity()
    {
        var gravity = new GravityProcessor();
        _board.SetItem(3, 2, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        gravity.ApplyGravity(_board);

        Assert.IsTrue(_board.GetItem(3, 0).IsRocket);
        Assert.IsTrue(_board.GetItem(3, 2).IsEmpty);
    }

    [Test]
    public void Rocket_BlockedByNonMovableObstacle()
    {
        var gravity = new GravityProcessor();
        _board.SetItem(3, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        gravity.ApplyGravity(_board);

        Assert.IsTrue(_board.GetItem(3, 2).IsRocket);
    }
}
