using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tests specifically for the ParallelPaths combo fix.
/// Existing RocketExplosionTests cover damage correctness.
/// These tests verify the PATH STRUCTURE is correct for View animation.
/// </summary>
[TestFixture]
public class ComboParallelPathsTests
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
    // SINGLE ROCKET: IsCombo = false
    // ==========================================

    [Test]
    public void ExplodeRocket_Single_IsComboFalse()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsFalse(result.IsCombo, "Single rocket should not be a combo");
        Assert.IsNull(result.ParallelPathsA, "Single rocket should have null ParallelPathsA");
        Assert.IsNull(result.ParallelPathsB, "Single rocket should have null ParallelPathsB");
    }

    [Test]
    public void ExplodeRocket_Single_UsesPathAPathB()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsTrue(result.PathA.Count > 0, "Single rocket should populate PathA");
        Assert.IsTrue(result.PathB.Count > 0, "Single rocket should populate PathB");
    }

    [Test]
    public void ExplodeRocket_Vertical_Single_IsComboFalse()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        var result = _processor.ExplodeRocket(_board, new Coordinate(3, 3));

        Assert.IsFalse(result.IsCombo);
    }

    // ==========================================
    // COMBO: IsCombo = true, 3 parallel paths
    // ==========================================

    [Test]
    public void ProcessCombo_IsComboTrue()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(explosions[0].IsCombo, "Horizontal combo should have IsCombo=true");
        Assert.IsTrue(explosions[1].IsCombo, "Vertical combo should have IsCombo=true");
    }

    [Test]
    public void ProcessCombo_HorizontalExplosion_Has3ParallelPathsEachDirection()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0];
        Assert.AreEqual(3, horiz.ParallelPathsA.Count, "Should have 3 paths going left");
        Assert.AreEqual(3, horiz.ParallelPathsB.Count, "Should have 3 paths going right");
    }

    [Test]
    public void ProcessCombo_VerticalExplosion_Has3ParallelPathsEachDirection()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var vert = explosions[1];
        Assert.AreEqual(3, vert.ParallelPathsA.Count, "Should have 3 paths going down");
        Assert.AreEqual(3, vert.ParallelPathsB.Count, "Should have 3 paths going up");
    }

    // ==========================================
    // PARALLEL PATHS: Each path stays in its own row/column
    // ==========================================

    [Test]
    public void ProcessCombo_HorizontalPaths_EachRowStaysInItsRow()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0];

        // ParallelPathsA: 3 rows going LEFT from origin
        // [0] = row y-1 = row 2
        // [1] = row y   = row 3
        // [2] = row y+1 = row 4

        foreach (var cell in horiz.ParallelPathsA[0])
            Assert.AreEqual(2, cell.y, $"Path[0] going left should stay in row y=2, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsA[1])
            Assert.AreEqual(3, cell.y, $"Path[1] going left should stay in row y=3, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsA[2])
            Assert.AreEqual(4, cell.y, $"Path[2] going left should stay in row y=4, got y={cell.y}");

        // ParallelPathsB: 3 rows going RIGHT from origin
        foreach (var cell in horiz.ParallelPathsB[0])
            Assert.AreEqual(2, cell.y, $"Path[0] going right should stay in row y=2, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsB[1])
            Assert.AreEqual(3, cell.y, $"Path[1] going right should stay in row y=3, got y={cell.y}");

        foreach (var cell in horiz.ParallelPathsB[2])
            Assert.AreEqual(4, cell.y, $"Path[2] going right should stay in row y=4, got y={cell.y}");
    }

    [Test]
    public void ProcessCombo_VerticalPaths_EachColumnStaysInItsColumn()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var vert = explosions[1];

        // ParallelPathsA: 3 columns going DOWN
        // [0] = col x-1 = col 2
        // [1] = col x   = col 3
        // [2] = col x+1 = col 4

        foreach (var cell in vert.ParallelPathsA[0])
            Assert.AreEqual(2, cell.x, $"Path[0] going down should stay in col x=2, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsA[1])
            Assert.AreEqual(3, cell.x, $"Path[1] going down should stay in col x=3, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsA[2])
            Assert.AreEqual(4, cell.x, $"Path[2] going down should stay in col x=4, got x={cell.x}");

        // ParallelPathsB: 3 columns going UP
        foreach (var cell in vert.ParallelPathsB[0])
            Assert.AreEqual(2, cell.x, $"Path[0] going up should stay in col x=2, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsB[1])
            Assert.AreEqual(3, cell.x, $"Path[1] going up should stay in col x=3, got x={cell.x}");

        foreach (var cell in vert.ParallelPathsB[2])
            Assert.AreEqual(4, cell.x, $"Path[2] going up should stay in col x=4, got x={cell.x}");
    }

    // ==========================================
    // PARALLEL PATHS: Correct traversal order
    // ==========================================

    [Test]
    public void ProcessCombo_HorizontalPathsA_CellsGoLeftward()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        // Center row going left: cells should have decreasing x
        var centerPathLeft = explosions[0].ParallelPathsA[1]; // row y=3

        for (int i = 1; i < centerPathLeft.Count; i++)
        {
            Assert.Less(centerPathLeft[i].x, centerPathLeft[i - 1].x,
                "Cells going left should have decreasing x values");
        }
    }

    [Test]
    public void ProcessCombo_HorizontalPathsB_CellsGoRightward()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var centerPathRight = explosions[0].ParallelPathsB[1]; // row y=3

        for (int i = 1; i < centerPathRight.Count; i++)
        {
            Assert.Greater(centerPathRight[i].x, centerPathRight[i - 1].x,
                "Cells going right should have increasing x values");
        }
    }

    [Test]
    public void ProcessCombo_VerticalPathsA_CellsGoDownward()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var centerPathDown = explosions[1].ParallelPathsA[1]; // col x=3

        for (int i = 1; i < centerPathDown.Count; i++)
        {
            Assert.Less(centerPathDown[i].y, centerPathDown[i - 1].y,
                "Cells going down should have decreasing y values");
        }
    }

    // ==========================================
    // EDGE CASES: Board boundaries
    // ==========================================

    [Test]
    public void ProcessCombo_AtTopEdge_ClipsOutOfBoundsRows()
    {
        // Combo at (3, 7) — top edge of 8x8 board
        // Row y+1 = y=8 is out of bounds
        _board.SetItem(3, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 7, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 7),
            new List<Coordinate> { new Coordinate(4, 7) });

        var horiz = explosions[0];
        Assert.AreEqual(3, horiz.ParallelPathsA.Count, "Still 3 path lists");
        Assert.AreEqual(3, horiz.ParallelPathsB.Count, "Still 3 path lists");

        // Row y=6 should have cells, row y=7 should have cells,
        // row y=8 should be empty (out of bounds)
        Assert.IsTrue(horiz.ParallelPathsA[0].Count > 0, "Row y=6 should have cells going left");
        Assert.IsTrue(horiz.ParallelPathsA[1].Count > 0, "Row y=7 should have cells going left");
        Assert.AreEqual(0, horiz.ParallelPathsA[2].Count, "Row y=8 out of bounds — empty path");
    }

    [Test]
    public void ProcessCombo_AtBottomEdge_ClipsOutOfBoundsRows()
    {
        // Combo at (3, 0) — bottom edge
        _board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 0, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 0),
            new List<Coordinate> { new Coordinate(4, 0) });

        var horiz = explosions[0];
        // Row y-1 = y=-1 is out of bounds
        Assert.AreEqual(0, horiz.ParallelPathsA[0].Count, "Row y=-1 out of bounds — empty path");
        Assert.IsTrue(horiz.ParallelPathsA[1].Count > 0, "Row y=0 should have cells");
        Assert.IsTrue(horiz.ParallelPathsA[2].Count > 0, "Row y=1 should have cells");
    }

    [Test]
    public void ProcessCombo_AtLeftEdge_ClipsOutOfBoundsColumns()
    {
        // Combo at (0, 3)
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(1, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(0, 3),
            new List<Coordinate> { new Coordinate(1, 3) });

        var vert = explosions[1];
        // Col x-1 = x=-1 is out of bounds
        Assert.AreEqual(0, vert.ParallelPathsA[0].Count, "Col x=-1 out of bounds — empty path");
        Assert.IsTrue(vert.ParallelPathsA[1].Count > 0, "Col x=0 should have cells");
        Assert.IsTrue(vert.ParallelPathsA[2].Count > 0, "Col x=1 should have cells");
    }

    [Test]
    public void ProcessCombo_AtCorner_MultipleEdgesClipped()
    {
        // Combo at (0, 0) — bottom-left corner
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(0, 0),
            new List<Coordinate> { new Coordinate(1, 0) });

        // Should not throw
        Assert.AreEqual(2, explosions.Count);
        Assert.IsTrue(explosions[0].IsCombo);
        Assert.IsTrue(explosions[1].IsCombo);
    }

    // ==========================================
    // DAMAGE: Still correct with new path structure
    // ==========================================

    [Test]
    public void ProcessCombo_CubesInAllThreeRows_AllDestroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        // Cubes in 3 horizontal rows, beyond the 3×3 area
        PlaceCube(0, 2, ItemIds.Red);   // row y=2 (y-1)
        PlaceCube(0, 3, ItemIds.Blue);  // row y=3 (center)
        PlaceCube(0, 4, ItemIds.Green); // row y=4 (y+1)
        PlaceCube(7, 2, ItemIds.Red);   // row y=2 right side
        PlaceCube(7, 3, ItemIds.Blue);  // row y=3 right side
        PlaceCube(7, 4, ItemIds.Green); // row y=4 right side

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty, "Row y-1 left cube destroyed");
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty, "Row y center left cube destroyed");
        Assert.IsTrue(_board.GetItem(0, 4).IsEmpty, "Row y+1 left cube destroyed");
        Assert.IsTrue(_board.GetItem(7, 2).IsEmpty, "Row y-1 right cube destroyed");
        Assert.IsTrue(_board.GetItem(7, 3).IsEmpty, "Row y center right cube destroyed");
        Assert.IsTrue(_board.GetItem(7, 4).IsEmpty, "Row y+1 right cube destroyed");
    }

    [Test]
    public void ProcessCombo_CubesInAllThreeColumns_AllDestroyed()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        // Cubes in 3 vertical columns, beyond the 3×3 area
        PlaceCube(2, 0, ItemIds.Red);   // col x=2 (x-1)
        PlaceCube(3, 0, ItemIds.Blue);  // col x=3 (center)
        PlaceCube(4, 0, ItemIds.Green); // col x=4 (x+1)
        PlaceCube(2, 7, ItemIds.Red);   // col x=2 top
        PlaceCube(3, 7, ItemIds.Blue);  // col x=3 top
        PlaceCube(4, 7, ItemIds.Green); // col x=4 top

        _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty, "Col x-1 bottom cube destroyed");
        Assert.IsTrue(_board.GetItem(3, 0).IsEmpty, "Col x center bottom cube destroyed");
        Assert.IsTrue(_board.GetItem(4, 0).IsEmpty, "Col x+1 bottom cube destroyed");
        Assert.IsTrue(_board.GetItem(2, 7).IsEmpty, "Col x-1 top cube destroyed");
        Assert.IsTrue(_board.GetItem(3, 7).IsEmpty, "Col x center top cube destroyed");
        Assert.IsTrue(_board.GetItem(4, 7).IsEmpty, "Col x+1 top cube destroyed");
    }

    // ==========================================
    // PATH CONTENT: Includes 3x3 cells for visual traversal
    // ==========================================

    [Test]
    public void ProcessCombo_ParallelPaths_Include3x3CellsForVisualTraversal()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var explosions = _processor.ProcessCombo(_board, new Coordinate(3, 3),
            new List<Coordinate> { new Coordinate(4, 3) });

        var horiz = explosions[0];

        // The center row going left: first cell should be (2,3) — inside 3×3 area
        // The projectile needs to visually pass through this cell even though
        // damage was already applied by the 3×3 pass
        var centerLeft = horiz.ParallelPathsA[1]; // row y=3 going left
        if (centerLeft.Count > 0)
        {
            Assert.AreEqual(2, centerLeft[0].x,
                "First cell in center-left path should be x=2 (inside 3×3 for visual continuity)");
        }
    }

    // ==========================================
    // COMPATIBILITY: Old fields still populated for combo
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

        // Horizontal explosion should have destroyed cubes
        int totalDestroyed = explosions[0].DestroyedCubes.Count + explosions[1].DestroyedCubes.Count;
        Assert.IsTrue(totalDestroyed >= 2, "DestroyedCubes should still be populated");
    }
}
