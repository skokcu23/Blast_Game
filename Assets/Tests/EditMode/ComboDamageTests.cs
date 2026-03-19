using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Single source of truth for ALL combo behavior.
///
/// Core design: a combo fires two independent rockets (H and V) from the same origin.
/// Each direction traces 3 parallel paths with its OWN processed set.
/// The 3×3 center is their natural intersection — cells get hit by both:
///   - Cube/Box/Stone (1HP): H destroys → V sees empty → 1 hit total
///   - Vase (2HP): H damages (2→1) → V destroys (1→0) → 2 hits total
///
/// TracePathCombo starts AT the origin cell (not origin + direction).
/// This ensures every 3×3 cell is visited by both H and V for dual damage.
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
    // BASIC STRUCTURE
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
    // PATH STRUCTURE: Each path stays in its own row/column
    // ==========================================

    [Test]
    public void ProcessCombo_HorizontalPaths_EachRowStaysInItsRow()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) })[0];

        // ParallelPathsA: going LEFT. [0]=row y-1=2, [1]=row y=3, [2]=row y+1=4
        foreach (var cell in horiz.ParallelPathsA[0])
            Assert.AreEqual(2, cell.y, $"H left path[0] should stay in row y=2, got y={cell.y}");
        foreach (var cell in horiz.ParallelPathsA[1])
            Assert.AreEqual(3, cell.y, $"H left path[1] should stay in row y=3, got y={cell.y}");
        foreach (var cell in horiz.ParallelPathsA[2])
            Assert.AreEqual(4, cell.y, $"H left path[2] should stay in row y=4, got y={cell.y}");

        // ParallelPathsB: going RIGHT
        foreach (var cell in horiz.ParallelPathsB[0])
            Assert.AreEqual(2, cell.y, $"H right path[0] should stay in row y=2, got y={cell.y}");
        foreach (var cell in horiz.ParallelPathsB[1])
            Assert.AreEqual(3, cell.y, $"H right path[1] should stay in row y=3, got y={cell.y}");
        foreach (var cell in horiz.ParallelPathsB[2])
            Assert.AreEqual(4, cell.y, $"H right path[2] should stay in row y=4, got y={cell.y}");
    }

    [Test]
    public void ProcessCombo_VerticalPaths_EachColumnStaysInItsColumn()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var vert = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) })[1];

        // ParallelPathsA: going DOWN. [0]=col x-1=2, [1]=col x=3, [2]=col x+1=4
        foreach (var cell in vert.ParallelPathsA[0])
            Assert.AreEqual(2, cell.x, $"V down path[0] should stay in col x=2, got x={cell.x}");
        foreach (var cell in vert.ParallelPathsA[1])
            Assert.AreEqual(3, cell.x, $"V down path[1] should stay in col x=3, got x={cell.x}");
        foreach (var cell in vert.ParallelPathsA[2])
            Assert.AreEqual(4, cell.x, $"V down path[2] should stay in col x=4, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsB[0])
            Assert.AreEqual(2, cell.x, $"V up path[0] should stay in col x=2, got x={cell.x}");
        foreach (var cell in vert.ParallelPathsB[1])
            Assert.AreEqual(3, cell.x, $"V up path[1] should stay in col x=3, got x={cell.x}");
        foreach (var cell in vert.ParallelPathsB[2])
            Assert.AreEqual(4, cell.x, $"V up path[2] should stay in col x=4, got x={cell.x}");
    }

    [Test]
    public void ProcessCombo_PathsIncludeOriginCell()
    {
        // TracePathCombo starts AT origin, so the first cell in center paths is the origin itself
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) })[0];

        // Center row (y=3) going left: starts at (3,3) — the origin itself
        var centerLeft = horiz.ParallelPathsA[1];
        Assert.IsTrue(centerLeft.Count > 0);
        Assert.AreEqual(3, centerLeft[0].x,
            "Center-left path starts at origin x=3 (TracePathCombo starts AT origin)");
        Assert.AreEqual(3, centerLeft[0].y);
    }

    [Test]
    public void ProcessCombo_HorizontalPathsA_CellsGoLeftward()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) })[0];

        var centerLeft = horiz.ParallelPathsA[1];
        for (int i = 1; i < centerLeft.Count; i++)
            Assert.Less(centerLeft[i].x, centerLeft[i - 1].x, "Cells going left should have decreasing x");
    }

    [Test]
    public void ProcessCombo_HorizontalPathsB_CellsGoRightward()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) })[0];

        var centerRight = horiz.ParallelPathsB[1];
        for (int i = 1; i < centerRight.Count; i++)
            Assert.Greater(centerRight[i].x, centerRight[i - 1].x, "Cells going right should have increasing x");
    }

    // ==========================================
    // CUBES IN 3×3 — Die on first hit
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

    [Test]
    public void ProcessCombo_CubesBeyond3x3_AllDestroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        PlaceCube(0, 3, ItemIds.Red);
        PlaceCube(7, 3, ItemIds.Blue);
        PlaceCube(3, 0, ItemIds.Green);
        PlaceCube(3, 7, ItemIds.Yellow);
        PlaceCube(0, 2, ItemIds.Red);
        PlaceCube(7, 4, ItemIds.Blue);
        PlaceCube(2, 0, ItemIds.Green);
        PlaceCube(4, 7, ItemIds.Yellow);

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(7, 3).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 0).IsEmpty);
        Assert.IsTrue(_board.GetItem(3, 7).IsEmpty);
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty);
        Assert.IsTrue(_board.GetItem(7, 4).IsEmpty);
        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty);
        Assert.IsTrue(_board.GetItem(4, 7).IsEmpty);
    }

    [Test]
    public void ProcessCombo_CubeIn3x3_HorizDestroys_VertPassesThrough()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(2, 2, ItemIds.Red);

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        CollectionAssert.Contains(explosions[0].DestroyedCubes, new Coordinate(2, 2),
            "H should destroy the cube (processes first)");
        CollectionAssert.DoesNotContain(explosions[1].DestroyedCubes, new Coordinate(2, 2),
            "V should not see cube (already empty)");
    }

    // ==========================================
    // VASE IN 3×3 — Takes 2 damage (killed by H+V)
    // ==========================================

    [Test]
    public void ProcessCombo_VaseIn3x3Corner_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 2).IsEmpty,
            "Vase in 3×3 destroyed: H hits (2→1), V hits (1→0)");
    }

    [Test]
    public void ProcessCombo_VaseAtIntersection_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(3, 4).IsEmpty,
            "Vase at H/V intersection destroyed by dual-direction damage");
    }

    [Test]
    public void ProcessCombo_VaseIn3x3_HorizDamages_VertDestroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        CollectionAssert.Contains(explosions[0].DamagedObstacles, new Coordinate(2, 2),
            "H should damage vase (first hit, 2→1)");
        CollectionAssert.Contains(explosions[1].DestroyedObstacles, new Coordinate(2, 2),
            "V should destroy vase (second hit, 1→0)");
    }

    // ==========================================
    // VASE OUTSIDE 3×3 — Only one direction, takes 1 damage
    // ==========================================

    [Test]
    public void ProcessCombo_VaseOnHorizontalPathOnly_Survives()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.AreEqual(1, _board.GetItem(0, 3).Health, "Only H reaches x=0. 1 hit.");
        Assert.IsTrue(_board.GetItem(0, 3).IsAlive);
    }

    [Test]
    public void ProcessCombo_VaseOnVerticalPathOnly_Survives()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 7, ItemFactory.CreateItem(ItemIds.Vase));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.AreEqual(1, _board.GetItem(3, 7).Health, "Only V reaches y=7. 1 hit.");
        Assert.IsTrue(_board.GetItem(3, 7).IsAlive);
    }

    // ==========================================
    // BOX AND STONE IN 3×3
    // ==========================================

    [Test]
    public void ProcessCombo_BoxIn3x3_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 4, ItemFactory.CreateItem(ItemIds.Box));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 4).IsEmpty);
    }

    [Test]
    public void ProcessCombo_StoneIn3x3_Destroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(4, 2, ItemFactory.CreateItem(ItemIds.Stone));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(4, 2).IsEmpty);
    }

    // ==========================================
    // TRIGGERED ROCKETS
    // ==========================================

    [Test]
    public void ProcessCombo_RocketIn3x3_TriggeredNotRemoved()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 4).IsRocket, "Must stay on board for chain reaction");

        var allTriggered = explosions.SelectMany(e => e.TriggeredRockets).ToList();
        CollectionAssert.Contains(allTriggered, new Coordinate(2, 4));
    }

    [Test]
    public void ProcessCombo_RocketOnPath_TriggeredNotRemoved()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(7, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(7, 3).IsRocket);
    }

    [Test]
    public void ProcessCombo_TriggeredRocket_NotDuplicated()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var allTriggered = explosions.SelectMany(e => e.TriggeredRockets).ToList();
        int count = allTriggered.Count(c => c == new Coordinate(2, 2));
        Assert.AreEqual(1, count, "Shared triggeredSet prevents duplicates across H and V");
    }

    // ==========================================
    // COMBO AREA CLEARED (View data)
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
    public void ProcessCombo_ComboAreaCleared_ContainsDestroyedVase()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        CollectionAssert.Contains(explosions[0].ComboAreaCleared, new Coordinate(2, 2),
            "Vase killed by dual hit should be in ComboAreaCleared");
    }

    [Test]
    public void ProcessCombo_ComboAreaCleared_SharedBetweenExplosions()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.AreSame(explosions[0].ComboAreaCleared, explosions[1].ComboAreaCleared);
    }

    // ==========================================
    // EDGE CASES: Board boundaries
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
            Assert.IsTrue(explosions[0].IsCombo);
        });
    }

    [Test]
    public void ProcessCombo_AtTopEdge_OutOfBoundsRowsEmpty()
    {
        _board.SetItem(3, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 7, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 7),
            new List<Coordinate> { new Coordinate(4, 7) })[0];

        // Row y+1 = y=8 is out of bounds → TracePathCombo starts at (3,8),
        // IsValidCoordinate fails → empty path
        Assert.AreEqual(0, horiz.ParallelPathsA[2].Count, "Row y=8 should be empty");
        Assert.AreEqual(0, horiz.ParallelPathsB[2].Count, "Row y=8 should be empty");
        Assert.IsTrue(horiz.ParallelPathsA[0].Count > 0, "Row y=6 should have cells");
        Assert.IsTrue(horiz.ParallelPathsA[1].Count > 0, "Row y=7 should have cells");
    }

    [Test]
    public void ProcessCombo_AtBottomEdge_OutOfBoundsRowsEmpty()
    {
        _board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 0, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var horiz = _processor.ProcessCombo(_board, new Coordinate(3, 0),
            new List<Coordinate> { new Coordinate(4, 0) })[0];

        Assert.AreEqual(0, horiz.ParallelPathsA[0].Count, "Row y=-1 should be empty");
        Assert.IsTrue(horiz.ParallelPathsA[1].Count > 0, "Row y=0 should have cells");
        Assert.IsTrue(horiz.ParallelPathsA[2].Count > 0, "Row y=1 should have cells");
    }

    [Test]
    public void ProcessCombo_AtLeftEdge_OutOfBoundsColumnsEmpty()
    {
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(1, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var vert = _processor.ProcessCombo(_board, new Coordinate(0, 3),
            new List<Coordinate> { new Coordinate(1, 3) })[1];

        Assert.AreEqual(0, vert.ParallelPathsA[0].Count, "Col x=-1 should be empty");
        Assert.IsTrue(vert.ParallelPathsA[1].Count > 0, "Col x=0 should have cells");
        Assert.IsTrue(vert.ParallelPathsA[2].Count > 0, "Col x=1 should have cells");
    }

    // ==========================================
    // DESTROYED CUBES STILL POPULATED
    // ==========================================

    [Test]
    public void ProcessCombo_DestroyedCubes_StillPopulated()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(0, 3, ItemIds.Red);
        PlaceCube(7, 3, ItemIds.Blue);

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        int total = explosions[0].DestroyedCubes.Count + explosions[1].DestroyedCubes.Count;
        Assert.IsTrue(total >= 2, "DestroyedCubes lists should be populated");
    }
}
