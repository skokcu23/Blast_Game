using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tests for the combo damage system.
///
/// Core principle: a combo fires two independent rockets (H and V).
/// The 3×3 center is their natural intersection — cells there get
/// hit by both directions. Vases (2HP) die. Everything else (1HP) dies
/// on the first hit; the second direction sees empty and passes through.
/// </summary>
[TestFixture]
public class ComboDamageTests
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
    // BASIC COMBO STRUCTURE
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
    public void ProcessCombo_ReturnsTwoExplosions_HorizontalFirst()
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
    public void ProcessCombo_BothExplosionsAreCombo()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(explosions[0].IsCombo);
        Assert.IsTrue(explosions[1].IsCombo);
    }

    [Test]
    public void ProcessCombo_Has3ParallelPathsPerDirection()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.AreEqual(3, explosions[0].ParallelPathsA.Count);
        Assert.AreEqual(3, explosions[0].ParallelPathsB.Count);
        Assert.AreEqual(3, explosions[1].ParallelPathsA.Count);
        Assert.AreEqual(3, explosions[1].ParallelPathsB.Count);
    }

    // ==========================================
    // CUBES IN 3×3 — Die on first hit (H), V sees empty
    // ==========================================

    [Test]
    public void ProcessCombo_CubesIn3x3_AllDestroyed()
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

    // ==========================================
    // CUBES BEYOND 3×3 — Destroyed by one direction
    // ==========================================

    [Test]
    public void ProcessCombo_CubesBeyond3x3_AllDestroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        // Far cubes on all 4 axes
        PlaceCube(0, 3, ItemIds.Red);   // H row center, far left
        PlaceCube(7, 3, ItemIds.Blue);  // H row center, far right
        PlaceCube(3, 0, ItemIds.Green); // V col center, far bottom
        PlaceCube(3, 7, ItemIds.Yellow);// V col center, far top

        // Cubes on outer rows/columns of combo
        PlaceCube(0, 2, ItemIds.Red);   // H row y-1, far left
        PlaceCube(7, 4, ItemIds.Blue);  // H row y+1, far right
        PlaceCube(2, 0, ItemIds.Green); // V col x-1, far bottom
        PlaceCube(4, 7, ItemIds.Yellow);// V col x+1, far top

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty, "H center row far left");
        Assert.IsTrue(_board.GetItem(7, 3).IsEmpty, "H center row far right");
        Assert.IsTrue(_board.GetItem(3, 0).IsEmpty, "V center col far bottom");
        Assert.IsTrue(_board.GetItem(3, 7).IsEmpty, "V center col far top");
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty, "H outer row far left");
        Assert.IsTrue(_board.GetItem(7, 4).IsEmpty, "H outer row far right");
        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty, "V outer col far bottom");
        Assert.IsTrue(_board.GetItem(4, 7).IsEmpty, "V outer col far top");
    }

    // ==========================================
    // VASE IN 3×3 — Takes 2 damage (killed by H+V intersection)
    // ==========================================

    [Test]
    public void ProcessCombo_VaseIn3x3_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase)); // 2HP, in 3×3

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 2).IsEmpty,
            "Vase in 3×3 should be destroyed: H hits (2→1), V hits (1→0)");
    }

    [Test]
    public void ProcessCombo_VaseOn3x3Edge_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.Vase)); // On H path AND V path

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(3, 4).IsEmpty,
            "Vase at intersection of H row y=4 and V col x=3 should be destroyed");
    }

    // ==========================================
    // VASE OUTSIDE 3×3 — Only on one direction, takes 1 damage
    // ==========================================

    [Test]
    public void ProcessCombo_VaseOnHorizontalPathOnly_TakesOneDamage()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Vase)); // Far left, H row center only

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Only H reaches it. V columns are x=2,3,4 — doesn't reach x=0
        Assert.AreEqual(1, _board.GetItem(0, 3).Health,
            "Vase on H path only should take 1 damage (2→1)");
        Assert.IsTrue(_board.GetItem(0, 3).IsAlive);
    }

    [Test]
    public void ProcessCombo_VaseOnVerticalPathOnly_TakesOneDamage()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 7, ItemFactory.CreateItem(ItemIds.Vase)); // Far top, V col center only

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Only V reaches it. H rows are y=2,3,4 — doesn't reach y=7
        Assert.AreEqual(1, _board.GetItem(3, 7).Health,
            "Vase on V path only should take 1 damage (2→1)");
        Assert.IsTrue(_board.GetItem(3, 7).IsAlive);
    }

    // ==========================================
    // BOX AND STONE IN 3×3 — Destroyed by first hit
    // ==========================================

    [Test]
    public void ProcessCombo_BoxIn3x3_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 4, ItemFactory.CreateItem(ItemIds.Box)); // 1HP

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 4).IsEmpty, "Box (1HP) destroyed by first hit");
    }

    [Test]
    public void ProcessCombo_StoneIn3x3_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(4, 2, ItemFactory.CreateItem(ItemIds.Stone)); // 1HP

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(4, 2).IsEmpty,
            "Stone (1HP) destroyed — combo uses Rocket damage which affects Stone");
    }

    // ==========================================
    // TRIGGERED ROCKETS — Still on board, shared dedup
    // ==========================================

    [Test]
    public void ProcessCombo_RocketIn3x3_IsTriggeredNotRemoved()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket)); // In 3×3

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 4).IsRocket,
            "Triggered rocket in 3×3 must stay on board for chain reaction");

        var allTriggered = new List<Coordinate>();
        foreach (var exp in explosions)
            allTriggered.AddRange(exp.TriggeredRockets);

        CollectionAssert.Contains(allTriggered, new Coordinate(2, 4));
    }

    [Test]
    public void ProcessCombo_RocketOnPath_IsTriggeredNotRemoved()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(7, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket)); // Far right on H path

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(7, 3).IsRocket,
            "Triggered rocket on path must stay on board");
    }

    [Test]
    public void ProcessCombo_TriggeredRocket_NotDuplicated()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        // Rocket at (2,2) — reachable by both H row y=2 and V col x=2
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var allTriggered = new List<Coordinate>();
        foreach (var exp in explosions)
            allTriggered.AddRange(exp.TriggeredRockets);

        int count = allTriggered.Count(c => c == new Coordinate(2, 2));
        Assert.AreEqual(1, count,
            "Shared triggeredSet prevents same rocket being queued twice across H and V");
    }

    // ==========================================
    // EDGE CASES — Board boundaries
    // ==========================================

    [Test]
    public void ProcessCombo_AtCorner_NoErrors()
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

    [Test]
    public void ProcessCombo_AtTopEdge_OutOfBoundsRowsEmpty()
    {
        _board.SetItem(3, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 7, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 7),
            new List<Coordinate> { new Coordinate(4, 7) });

        // Row y+1 = y=8 is out of bounds
        Assert.AreEqual(0, explosions[0].ParallelPathsA[2].Count, "Row y=8 path should be empty");
        Assert.AreEqual(0, explosions[0].ParallelPathsB[2].Count, "Row y=8 path should be empty");
    }

    // ==========================================
    // COMBO AREA CLEARED — View data
    // ==========================================

    [Test]
    public void ProcessCombo_ComboAreaCleared_ContainsRemovedRockets()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        CollectionAssert.Contains(explosions[0].ComboAreaCleared, new Coordinate(3, 3));
        CollectionAssert.Contains(explosions[0].ComboAreaCleared, new Coordinate(4, 3));
    }

    [Test]
    public void ProcessCombo_ComboAreaCleared_ContainsDestroyedCubes()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(2, 2, ItemIds.Red); // In 3×3

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        CollectionAssert.Contains(explosions[0].ComboAreaCleared, new Coordinate(2, 2),
            "Destroyed cube in 3×3 should be in ComboAreaCleared");
    }

    [Test]
    public void ProcessCombo_ComboAreaCleared_ContainsDestroyedVase()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase)); // 2HP in 3×3

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Vase takes 2 damage (H+V), dies. Cell is now empty.
        CollectionAssert.Contains(explosions[0].ComboAreaCleared, new Coordinate(2, 2),
            "Destroyed vase should be in ComboAreaCleared");
    }

    [Test]
    public void ProcessCombo_ComboAreaCleared_SharedBetweenExplosions()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Both explosions share the same ComboAreaCleared list
        Assert.AreSame(explosions[0].ComboAreaCleared, explosions[1].ComboAreaCleared);
    }

    // ==========================================
    // PARALLEL PATH STRUCTURE (unchanged from before)
    // ==========================================

    [Test]
    public void ProcessCombo_HorizontalPaths_StayInTheirRow()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0];

        foreach (var cell in horiz.ParallelPathsA[0])
            Assert.AreEqual(2, cell.y, $"H path[0] left should be row y=2, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsA[1])
            Assert.AreEqual(3, cell.y, $"H path[1] left should be row y=3, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsA[2])
            Assert.AreEqual(4, cell.y, $"H path[2] left should be row y=4, got y={cell.y}");
    }

    [Test]
    public void ProcessCombo_VerticalPaths_StayInTheirColumn()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var vert = explosions[1];

        foreach (var cell in vert.ParallelPathsA[0])
            Assert.AreEqual(2, cell.x, $"V path[0] down should be col x=2, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsA[1])
            Assert.AreEqual(3, cell.x, $"V path[1] down should be col x=3, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsA[2])
            Assert.AreEqual(4, cell.x, $"V path[2] down should be col x=4, got x={cell.x}");
    }

    // ==========================================
    // DAMAGE ATTRIBUTION — Which explosion owns what
    // ==========================================

    [Test]
    public void ProcessCombo_VaseIn3x3_HorizDamages_VertDestroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase)); // 2HP

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0]; // Traces first
        var vert = explosions[1];  // Traces second

        // H hits vase first: 2→1. Vase is in H's DamagedObstacles.
        CollectionAssert.Contains(horiz.DamagedObstacles, new Coordinate(2, 2),
            "H should have damaged the vase (first hit, 2→1)");

        // V hits vase second: 1→0. Vase is in V's DestroyedObstacles.
        CollectionAssert.Contains(vert.DestroyedObstacles, new Coordinate(2, 2),
            "V should have destroyed the vase (second hit, 1→0)");

        // Board cell is empty
        Assert.IsTrue(_board.GetItem(2, 2).IsEmpty);
    }

    [Test]
    public void ProcessCombo_CubeIn3x3_HorizDestroys_VertPassesThrough()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(2, 2, ItemIds.Red);

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0];
        var vert = explosions[1];

        // H destroys the cube (first direction processed)
        CollectionAssert.Contains(horiz.DestroyedCubes, new Coordinate(2, 2));

        // V doesn't see it (cell is empty by the time V processes it)
        CollectionAssert.DoesNotContain(vert.DestroyedCubes, new Coordinate(2, 2));
    }
}
