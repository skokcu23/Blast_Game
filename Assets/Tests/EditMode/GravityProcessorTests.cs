using NUnit.Framework;
using System.Linq;

[TestFixture]
public class GravityProcessorTests
{
    private Board _board;
    private GravityProcessor _gravity;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
        _gravity = new GravityProcessor();
    }

    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // BASIC GRAVITY
    // ==========================================

    [Test]
    public void ApplyGravity_CubeFallsIntoEmptyBelow()
    {
        PlaceCube(0, 1, ItemIds.Red);

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(1, movements.Count);
        Assert.AreEqual(new Coordinate(0, 1), movements[0].StartPos);
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 0).Id);
        Assert.IsTrue(_board.GetItem(0, 1).IsEmpty);
    }

    [Test]
    public void ApplyGravity_MultipleCubesFallInSameColumn()
    {
        PlaceCube(0, 1, ItemIds.Red);
        PlaceCube(0, 2, ItemIds.Blue);

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(2, movements.Count);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 1).Id);
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty);
    }

    [Test]
    public void ApplyGravity_CubeFallsOverMultipleEmptySpaces()
    {
        PlaceCube(0, 3, ItemIds.Green);

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(1, movements.Count);
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
        Assert.AreEqual(ItemIds.Green, _board.GetItem(0, 0).Id);
    }

    [Test]
    public void ApplyGravity_NoEmptySpaces_NoMovement()
    {
        for (int y = 0; y < _board.Height; y++)
            PlaceCube(0, y, ItemIds.Red);

        var movements = _gravity.ApplyGravity(_board);

        var col0Moves = movements.Where(m => m.StartPos.x == 0).ToList();
        Assert.AreEqual(0, col0Moves.Count);
    }

    // ==========================================
    // NON-MOVABLE BLOCKERS (BOX, STONE) — Gravity
    // ==========================================

    [Test]
    public void ApplyGravity_BoxBlocksFalling()
    {
        // empty y=0, Box y=1, Red y=2
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 2, ItemIds.Red);

        _gravity.ApplyGravity(_board);

        // Box stays, Red stays above box (no empty above box)
        Assert.AreEqual(ItemIds.Box, _board.GetItem(0, 1).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
    }

    [Test]
    public void ApplyGravity_StoneBlocksFalling()
    {
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Stone));
        PlaceCube(0, 2, ItemIds.Red);

        _gravity.ApplyGravity(_board);

        Assert.AreEqual(ItemIds.Stone, _board.GetItem(0, 1).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
    }

    [Test]
    public void ApplyGravity_CubeFallsToSpaceAboveBox()
    {
        // empty y=0, Box y=1, empty y=2, Red y=3
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 3, ItemIds.Red);

        _gravity.ApplyGravity(_board);

        // Red falls from y=3 to y=2 (lands on top of box)
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
    }

    [Test]
    public void ApplyGravity_MultipleCubesFallOntoBox()
    {
        // Box y=2, cubes at y=5, y=6 with empties between
        _board.SetItem(0, 2, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 5, ItemIds.Red);
        PlaceCube(0, 6, ItemIds.Blue);

        _gravity.ApplyGravity(_board);

        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 3).Id);
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 4).Id);
        Assert.IsTrue(_board.GetItem(0, 5).IsEmpty);
        Assert.IsTrue(_board.GetItem(0, 6).IsEmpty);
    }

    [Test]
    public void ApplyGravity_TwoObstaclesInColumn_CubesFallToCorrectSegments()
    {
        // Box y=1, Stone y=4, cubes at y=3 and y=6
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(0, 4, ItemFactory.CreateItem(ItemIds.Stone));
        PlaceCube(0, 3, ItemIds.Red);   // above box, below stone
        PlaceCube(0, 6, ItemIds.Blue);  // above stone

        _gravity.ApplyGravity(_board);

        // Red falls to y=2 (sits on box)
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
        // Blue falls to y=5 (sits on stone)
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 5).Id);
    }

    // ==========================================
    // MOVABLE OBSTACLES (VASE)
    // ==========================================

    [Test]
    public void ApplyGravity_VaseFallsDown()
    {
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Vase));

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(1, movements.Count);
        Assert.AreEqual(ItemIds.Vase, _board.GetItem(0, 0).Id);
        Assert.IsTrue(_board.GetItem(0, 1).IsEmpty);
    }

    [Test]
    public void ApplyGravity_VaseBlockedByBox()
    {
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Vase));

        _gravity.ApplyGravity(_board);

        // Vase falls to y=2 (sits on box)
        Assert.AreEqual(ItemIds.Vase, _board.GetItem(0, 2).Id);
    }

    // ==========================================
    // REFILL — Basic
    // ==========================================

    [Test]
    public void FillEmptySpaces_FillsAllEmpties_NoObstacles()
    {
        var newItems = _gravity.FillEmptySpaces(_board);

        var col0Items = newItems.Where(m => m.EndPos.x == 0).ToList();
        Assert.AreEqual(_board.Height, col0Items.Count);

        foreach (var movement in newItems)
        {
            var item = _board.GetItem(movement.EndPos);
            Assert.IsTrue(item.IsCube, $"Refill produced non-cube: {item.Id}");
        }
    }

    [Test]
    public void FillEmptySpaces_DoesNotOverwriteExistingItems()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(0, 1, ItemIds.Blue);

        var newItems = _gravity.FillEmptySpaces(_board);

        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 1).Id);

        var col0New = newItems.Where(m => m.EndPos.x == 0).ToList();
        Assert.AreEqual(6, col0New.Count);
    }

    [Test]
    public void FillEmptySpaces_SpawnPositionsAreAboveBoard()
    {
        var newItems = _gravity.FillEmptySpaces(_board);

        foreach (var movement in newItems)
        {
            Assert.GreaterOrEqual(movement.StartPos.y, _board.Height,
                $"Spawn position y={movement.StartPos.y} should be >= board height {_board.Height}");
        }
    }

    [Test]
    public void FillEmptySpaces_SpawnPositionsStaggeredInColumn()
    {
        var newItems = _gravity.FillEmptySpaces(_board);

        // Top-down scan: highest target y gets lowest spawn offset
        var col0 = newItems.Where(m => m.EndPos.x == 0)
            .OrderByDescending(m => m.EndPos.y).ToList();

        for (int i = 0; i < col0.Count; i++)
        {
            Assert.AreEqual(_board.Height + i, col0[i].StartPos.y,
                $"Spawn offset for item {i} (target y={col0[i].EndPos.y}) is wrong");
        }
    }

    // ==========================================
    // REFILL — Obstacle Barriers
    // ==========================================

    [Test]
    public void FillEmptySpaces_DoesNotFillBelowObstacle()
    {
        // Stone at y=3, empties below at y=0,1,2
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Stone));
        // Fill above stone so they're not empty
        for (int y = 4; y < _board.Height; y++)
            PlaceCube(0, y, ItemIds.Red);

        _gravity.FillEmptySpaces(_board);

        // Cells below stone should still be empty
        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty, "y=0 below stone should stay empty");
        Assert.IsTrue(_board.GetItem(0, 1).IsEmpty, "y=1 below stone should stay empty");
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty, "y=2 below stone should stay empty");
    }

    [Test]
    public void FillEmptySpaces_FillsAboveObstacle()
    {
        // Stone at y=3, empties above at y=4,5,6,7
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Stone));

        _gravity.FillEmptySpaces(_board);

        // Cells above stone should be filled
        Assert.IsFalse(_board.GetItem(0, 4).IsEmpty, "y=4 above stone should be filled");
        Assert.IsFalse(_board.GetItem(0, 5).IsEmpty, "y=5 above stone should be filled");
        Assert.IsFalse(_board.GetItem(0, 6).IsEmpty, "y=6 above stone should be filled");
        Assert.IsFalse(_board.GetItem(0, 7).IsEmpty, "y=7 above stone should be filled");

        // But cells below should NOT
        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty, "y=0 below stone stays empty");
        Assert.IsTrue(_board.GetItem(0, 1).IsEmpty, "y=1 below stone stays empty");
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty, "y=2 below stone stays empty");
    }

    [Test]
    public void FillEmptySpaces_ObstacleAtBottom_EverythingAboveFills()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));

        _gravity.FillEmptySpaces(_board);

        Assert.AreEqual(ItemIds.Box, _board.GetItem(0, 0).Id);
        for (int y = 1; y < _board.Height; y++)
            Assert.IsFalse(_board.GetItem(0, y).IsEmpty, $"y={y} above box should be filled");
    }

    [Test]
    public void FillEmptySpaces_ObstacleAtTop_NothingFills()
    {
        _board.SetItem(0, 7, ItemFactory.CreateItem(ItemIds.Stone));

        _gravity.FillEmptySpaces(_board);

        // Stone is at the very top. Scanning from top: y=7 is stone → break.
        // Everything below stays empty.
        Assert.AreEqual(ItemIds.Stone, _board.GetItem(0, 7).Id);
        for (int y = 0; y < 7; y++)
            Assert.IsTrue(_board.GetItem(0, y).IsEmpty, $"y={y} below top stone stays empty");
    }

    [Test]
    public void FillEmptySpaces_TwoObstacles_OnlyTopSegmentFills()
    {
        // Box y=1, Stone y=4
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(0, 4, ItemFactory.CreateItem(ItemIds.Stone));

        _gravity.FillEmptySpaces(_board);

        // Above stone (y=5,6,7): should be filled
        Assert.IsFalse(_board.GetItem(0, 5).IsEmpty);
        Assert.IsFalse(_board.GetItem(0, 6).IsEmpty);
        Assert.IsFalse(_board.GetItem(0, 7).IsEmpty);

        // Between obstacles (y=2,3): should stay empty (sealed)
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty, "Between obstacles stays empty");
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty, "Between obstacles stays empty");

        // Below bottom obstacle (y=0): should stay empty (sealed)
        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty, "Below all obstacles stays empty");
    }

    [Test]
    public void FillEmptySpaces_BoxWithCubeAbove_FillsRemainingGap()
    {
        // Box y=0, cube y=1, empties y=2..7
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 1, ItemIds.Red);

        var newItems = _gravity.FillEmptySpaces(_board);

        var col0 = newItems.Where(m => m.EndPos.x == 0).ToList();
        Assert.AreEqual(6, col0.Count, "Should fill y=2 through y=7");

        Assert.AreEqual(ItemIds.Box, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 1).Id);
    }

    [Test]
    public void FillEmptySpaces_RefilledItemsSpawnAboveBoard_NotThroughObstacle()
    {
        // Stone at y=3, empties at y=4..7
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Stone));

        var newItems = _gravity.FillEmptySpaces(_board);

        var col0 = newItems.Where(m => m.EndPos.x == 0).ToList();

        // All spawn positions should be above board
        foreach (var item in col0)
        {
            Assert.GreaterOrEqual(item.StartPos.y, _board.Height,
                "Refilled items should spawn above board");
            Assert.GreaterOrEqual(item.EndPos.y, 4,
                "Refilled items should only target cells above the obstacle");
        }
    }

    [Test]
    public void FillEmptySpaces_NoNewItemsInSealedSegments()
    {
        // Stone at middle of column
        _board.SetItem(0, 4, ItemFactory.CreateItem(ItemIds.Stone));

        var newItems = _gravity.FillEmptySpaces(_board);

        var col0 = newItems.Where(m => m.EndPos.x == 0).ToList();

        // Should only fill y=5,6,7 (3 items)
        Assert.AreEqual(3, col0.Count, "Only 3 cells above stone should be filled");

        foreach (var item in col0)
            Assert.IsTrue(item.EndPos.y > 4, $"EndPos y={item.EndPos.y} should be > 4 (above stone)");
    }

    // ==========================================
    // GRAVITY + REFILL FULL CYCLE
    // ==========================================

    [Test]
    public void FullCycle_NoObstacles_BoardIsFull()
    {
        _board.InitializeRandom();
        _board.SetItem(0, 0, ItemFactory.CreateEmpty());
        _board.SetItem(0, 1, ItemFactory.CreateEmpty());

        _gravity.ApplyGravity(_board);
        _gravity.FillEmptySpaces(_board);

        for (int x = 0; x < _board.Width; x++)
            for (int y = 0; y < _board.Height; y++)
                Assert.IsFalse(_board.GetItem(x, y).IsEmpty,
                    $"Cell ({x},{y}) is still empty after full cycle");
    }

    [Test]
    public void FullCycle_WithObstacle_CellsBelowStayEmpty()
    {
        // Fill board, place obstacle, blast cells above it
        for (int y = 0; y < _board.Height; y++)
            PlaceCube(0, y, ItemIds.Red);

        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Stone));

        // Blast cells above stone
        _board.SetItem(0, 5, ItemFactory.CreateEmpty());
        _board.SetItem(0, 6, ItemFactory.CreateEmpty());

        _gravity.ApplyGravity(_board);
        _gravity.FillEmptySpaces(_board);

        // Above stone: should all be filled
        for (int y = 4; y < _board.Height; y++)
            Assert.IsFalse(_board.GetItem(0, y).IsEmpty,
                $"Cell (0,{y}) above stone should be filled");

        // Stone should still be there
        Assert.AreEqual(ItemIds.Stone, _board.GetItem(0, 3).Id);

        // Below stone: should still have original cubes (nothing was blasted there)
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 1).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
    }

    [Test]
    public void FullCycle_BlastBelowObstacle_EmptiesStayEmpty()
    {
        // Fill column
        for (int y = 0; y < _board.Height; y++)
            PlaceCube(0, y, ItemIds.Blue);

        _board.SetItem(0, 4, ItemFactory.CreateItem(ItemIds.Stone));

        // Blast cells BELOW stone
        _board.SetItem(0, 1, ItemFactory.CreateEmpty());
        _board.SetItem(0, 2, ItemFactory.CreateEmpty());

        _gravity.ApplyGravity(_board);

        // y=3 cube falls to y=1 (above box-less area), y=2 is now empty
        // Actually: y=0 blue, y=1 empty, y=2 empty, y=3 blue
        // After gravity: emptyCount starts. y=0 blue, y=1 empty (count=1),
        // y=2 empty (count=2), y=3 blue falls to y=1. y=4 stone resets count.
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 1).Id);
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty);
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
        Assert.AreEqual(ItemIds.Stone, _board.GetItem(0, 4).Id);

        _gravity.FillEmptySpaces(_board);

        // y=2 and y=3 are below stone — should NOT be filled
        Assert.IsTrue(_board.GetItem(0, 2).IsEmpty,
            "y=2 below stone should remain empty after refill");
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty,
            "y=3 below stone should remain empty after refill");
    }

    [Test]
    public void FullCycle_ObstacleDestroyed_NextCycleCanFillThrough()
    {
        // Stone at y=3
        _board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.Stone));
        PlaceCube(0, 4, ItemIds.Red);
        PlaceCube(0, 5, ItemIds.Blue);

        // First cycle — stone blocks
        _gravity.ApplyGravity(_board);
        _gravity.FillEmptySpaces(_board);

        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty, "Below stone stays empty");

        // Now destroy the stone (simulating rocket hit)
        _board.SetItem(0, 3, ItemFactory.CreateEmpty());

        // Second cycle — no more barrier
        _gravity.ApplyGravity(_board);
        _gravity.FillEmptySpaces(_board);

        // Everything should now be filled
        for (int y = 0; y < _board.Height; y++)
            Assert.IsFalse(_board.GetItem(0, y).IsEmpty,
                $"Cell (0,{y}) should be filled after stone removed and cycle run");
    }
}