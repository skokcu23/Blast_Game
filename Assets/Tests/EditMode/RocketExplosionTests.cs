using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

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
    public void ExplodeRocket_ContinuesThroughEmptySpaces()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        PlaceCube(5, 3, ItemIds.Green); // Gap at (4,3)

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        CollectionAssert.Contains(result.DestroyedCubes, new Coordinate(5, 3));
    }

    [Test]
    public void ExplodeRocket_ContinuesPastDestroyedObstacles()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(7, 3, ItemIds.Red);

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(7, 3).IsEmpty);
    }

    // ==========================================
    // OBSTACLE DAMAGE FROM ROCKET
    // ==========================================

    [Test]
    public void ExplodeRocket_DamagesBox_DestroysIt()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Box));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        Assert.AreEqual(ItemIds.Box, result.DestroyedObstacleInfos[0].ObstacleId);
    }

    [Test]
    public void ExplodeRocket_DamagesStone_DestroysIt()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(6, 3, ItemFactory.CreateItem(ItemIds.Stone));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(_board.GetItem(6, 3).IsEmpty);
    }

    [Test]
    public void ExplodeRocket_DamagesVase_SurvivesFirstHit()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 5, ItemFactory.CreateItem(ItemIds.Vase));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.AreEqual(1, _board.GetItem(3, 5).Health);
        CollectionAssert.Contains(result.DamagedObstacles, new Coordinate(3, 5));
    }

    // ==========================================
    // CHAIN REACTIONS — THE CRITICAL TESTS
    // ==========================================

    // TEST GAP 1: Triggered rocket must stay on board
    [Test]
    public void ExplodeRocket_TriggeredRocket_StaysOnBoard()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(6, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        CollectionAssert.Contains(result.TriggeredRockets, new Coordinate(6, 3));
        // *** THE CRITICAL ASSERTION: rocket B must still be on the board ***
        Assert.IsTrue(_board.GetItem(6, 3).IsRocket,
            "Triggered rocket must remain on board for chain reaction queue to work");
    }

    // TEST GAP 2: Multi-step chain A → B → C
    [Test]
    public void ExplodeRocket_MultiStepChain_ABC()
    {
        // A (horizontal) at (0,3) → hits B (vertical) at (5,3) → hits C (horizontal) at (5,7)
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(5, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        // Step 1: Explode A
        var resultA = _processor.ExplodeRocket(_board, new Coordinate(0, 3));
        Assert.IsNotNull(resultA);
        CollectionAssert.Contains(resultA.TriggeredRockets, new Coordinate(5, 3));
        Assert.IsTrue(_board.GetItem(5, 3).IsRocket, "B stays on board");

        // Step 2: Explode B (simulating queue)
        var resultB = _processor.ExplodeRocket(_board, new Coordinate(5, 3));
        Assert.IsNotNull(resultB, "B should be explodable");
        CollectionAssert.Contains(resultB.TriggeredRockets, new Coordinate(5, 7));
        Assert.IsTrue(_board.GetItem(5, 7).IsRocket, "C stays on board");

        // Step 3: Explode C
        var resultC = _processor.ExplodeRocket(_board, new Coordinate(5, 7));
        Assert.IsNotNull(resultC, "C should be explodable");

        // All three cleared
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(5, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(5, 7).IsEmpty);
    }

    // TEST GAP 5: Duplicate trigger safety — same rocket enqueued twice
    [Test]
    public void ExplodeRocket_SameRocketNotTriggeredTwice()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        int count = result.TriggeredRockets.Count(c => c == new Coordinate(5, 3));
        Assert.AreEqual(1, count, "Same rocket should not appear twice in TriggeredRockets");
    }

    [Test]
    public void ExplodeRocket_AlreadyExplodedRocket_ReturnsNull()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        // Cell is now empty — second call should safely return null
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
        // Both should still be on board
        Assert.IsTrue(_board.GetItem(1, 3).IsRocket);
        Assert.IsTrue(_board.GetItem(6, 3).IsRocket);
    }

    // ==========================================
    // COMBO
    // ==========================================

    [Test]
    public void ProcessCombo_RemovesAllInvolvedRockets()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(3, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(4, 3).IsEmpty);
    }

    [Test]
    public void ProcessCombo_ReturnsTwoExplosions()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.AreEqual(2, explosions.Count);
        Assert.IsTrue(explosions[0].IsHorizontal);
        Assert.IsFalse(explosions[1].IsHorizontal);
    }

    [Test]
    public void ProcessCombo_Damages3x3Area()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        PlaceCube(2, 2, ItemIds.Red);
        PlaceCube(2, 3, ItemIds.Blue);
        PlaceCube(2, 4, ItemIds.Green);
        PlaceCube(3, 2, ItemIds.Yellow);
        PlaceCube(3, 4, ItemIds.Red);
        PlaceCube(4, 2, ItemIds.Blue);
        PlaceCube(4, 4, ItemIds.Green);

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 2).IsEmpty);
        Assert.IsTrue(_board.GetItem(2, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(2, 4).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 2).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 4).IsEmpty);
        Assert.IsTrue(_board.GetItem(4, 2).IsEmpty);
        Assert.IsTrue(_board.GetItem(4, 4).IsEmpty);
    }

    [Test]
    public void ProcessCombo_FiresBeyond3x3()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        PlaceCube(0, 3, ItemIds.Red);
        PlaceCube(7, 3, ItemIds.Blue);
        PlaceCube(3, 0, ItemIds.Green);
        PlaceCube(3, 7, ItemIds.Yellow);

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(7, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 0).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 7).IsEmpty);
    }

    // TEST GAP 3: Combo at board corner — 3×3 clips
    [Test]
    public void ProcessCombo_AtCorner_ClipsGracefully()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        Assert.DoesNotThrow(() =>
        {
            var explosions = _processor.ProcessCombo(_board, new Coordinate(0, 0),
                new List<Coordinate> { new Coordinate(1, 0) });
            Assert.AreEqual(2, explosions.Count);
        });
    }

    // TEST GAP 4: Rocket inside 3×3 area should be triggered
    [Test]
    public void ProcessCombo_RocketIn3x3Area_IsTriggered()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        // Third rocket inside the 3×3 area
        _board.SetItem(2, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // The 3×3 rocket should be triggered (still on board)
        Assert.IsTrue(_board.GetItem(2, 4).IsRocket,
            "Rocket in 3×3 area should stay on board for chain reaction");

        // And should appear in triggered rockets
        var allTriggered = new List<Coordinate>();
        foreach (var exp in explosions)
            allTriggered.AddRange(exp.TriggeredRockets);

        CollectionAssert.Contains(allTriggered, new Coordinate(2, 4));
    }

    [Test]
    public void ProcessCombo_TriggeredRocketInPath_StaysOnBoard()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(7, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(7, 3).IsRocket,
            "Triggered rocket from combo path must remain on board");
    }

    // TEST GAP 6: Vase at 3×3 corner takes exactly 1 Combo damage
    [Test]
    public void ProcessCombo_VaseAt3x3Corner_TakesExactlyOneDamage()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        // Vase at corner of 3×3 — NOT on any directional path
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Vase should have 1 HP remaining (took 1 Combo damage from 3×3)
        Assert.AreEqual(1, _board.GetItem(2, 2).Health);
        Assert.IsTrue(_board.GetItem(2, 2).IsAlive);
    }

    [Test]
    public void ProcessCombo_NoDuplicateDamage_VaseOnPathAnd3x3()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        // Vase at (3,4) — in 3×3 AND on vertical path
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Should only take 1 damage total (not 2)
        Assert.AreEqual(1, _board.GetItem(3, 4).Health);
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
