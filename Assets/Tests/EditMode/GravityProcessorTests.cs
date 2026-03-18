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

    // --- Helper ---
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
        // Column 0: empty at y=0, red at y=1
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
        // Column 0: empty at y=0, red at y=1, blue at y=2
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
        // Column 0: empty at y=0,1,2 — red at y=3
        PlaceCube(0, 3, ItemIds.Green);

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(1, movements.Count);
        Assert.AreEqual(new Coordinate(0, 0), movements[0].EndPos);
        Assert.AreEqual(ItemIds.Green, _board.GetItem(0, 0).Id);
    }

    [Test]
    public void ApplyGravity_NoEmptySpaces_NoMovement()
    {
        // Fill column 0 completely
        for (int y = 0; y < _board.Height; y++)
            PlaceCube(0, y, ItemIds.Red);

        var movements = _gravity.ApplyGravity(_board);

        // Should have zero movements (nothing to fall)
        var col0Moves = movements.Where(m => m.StartPos.x == 0).ToList();
        Assert.AreEqual(0, col0Moves.Count);
    }

    // ==========================================
    // NON-MOVABLE BLOCKERS (BOX, STONE)
    // ==========================================

    [Test]
    public void ApplyGravity_BoxBlocksFalling()
    {
        // Column 0: empty at y=0, Box at y=1 (immovable), Red at y=2
        _board.SetItem(1, 0, ItemFactory.CreateEmpty()); // ensure empty
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 2, ItemIds.Red);

        var movements = _gravity.ApplyGravity(_board);

        // Box stays at y=1, Red should NOT fall past the box
        Assert.AreEqual(ItemIds.Box, _board.GetItem(0, 1).Id);
        // Red was at y=2, box is at y=1 blocking — empty counter resets.
        // So red stays at y=2 (no empty space above the box from its perspective)
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
    }

    [Test]
    public void ApplyGravity_StoneBlocksFalling()
    {
        // Same as box test but with Stone
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Stone));
        PlaceCube(0, 2, ItemIds.Red);

        _gravity.ApplyGravity(_board);

        Assert.AreEqual(ItemIds.Stone, _board.GetItem(0, 1).Id);
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
    }

    [Test]
    public void ApplyGravity_CubeFallsToSpaceAboveBox()
    {
        // Column: empty at y=0, Box at y=1, empty at y=2, Red at y=3
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Box));
        PlaceCube(0, 3, ItemIds.Red);

        var movements = _gravity.ApplyGravity(_board);

        // Red should fall from y=3 to y=2 (lands on top of box)
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 2).Id);
        Assert.IsTrue(_board.GetItem(0, 3).IsEmpty);
    }

    // ==========================================
    // MOVABLE OBSTACLES (VASE)
    // ==========================================

    [Test]
    public void ApplyGravity_VaseFallsDown()
    {
        // Vases are movable — they should fall like cubes
        _board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Vase));

        var movements = _gravity.ApplyGravity(_board);

        Assert.AreEqual(1, movements.Count);
        Assert.AreEqual(ItemIds.Vase, _board.GetItem(0, 0).Id);
        Assert.IsTrue(_board.GetItem(0, 1).IsEmpty);
    }

    // ==========================================
    // REFILL
    // ==========================================

    [Test]
    public void FillEmptySpaces_FillsAllEmpties()
    {
        // Leave column 0 completely empty
        var newItems = _gravity.FillEmptySpaces(_board);

        // Should spawn items for every empty cell in the board
        int totalEmpty = _board.Width * _board.Height; // all cells were empty
        // After fill, column 0 should have 8 items spawned (board height)
        var col0Items = newItems.Where(m => m.EndPos.x == 0).ToList();
        Assert.AreEqual(_board.Height, col0Items.Count);

        // All spawned items should be cubes
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

        // The two existing cubes should still be there
        Assert.AreEqual(ItemIds.Red, _board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Blue, _board.GetItem(0, 1).Id);

        // New items should only fill y=2 through y=7
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
        // Clear a full column
        var newItems = _gravity.FillEmptySpaces(_board);

        // Check column 0 spawn positions are staggered
        var col0 = newItems.Where(m => m.EndPos.x == 0).OrderBy(m => m.EndPos.y).ToList();
        for (int i = 0; i < col0.Count; i++)
        {
            Assert.AreEqual(_board.Height + i, col0[i].StartPos.y,
                $"Spawn offset for item {i} in column 0 is wrong");
        }
    }

    // ==========================================
    // GRAVITY + REFILL FULL CYCLE
    // ==========================================

    [Test]
    public void FullCycle_BlastThenGravityThenRefill_BoardIsFull()
    {
        // Fill the board
        _board.InitializeRandom();

        // Blast the bottom two cells of column 0
        _board.SetItem(0, 0, ItemFactory.CreateEmpty());
        _board.SetItem(0, 1, ItemFactory.CreateEmpty());

        // Gravity
        _gravity.ApplyGravity(_board);

        // Refill
        _gravity.FillEmptySpaces(_board);

        // Every cell should now be occupied
        for (int x = 0; x < _board.Width; x++)
            for (int y = 0; y < _board.Height; y++)
                Assert.IsFalse(_board.GetItem(x, y).IsEmpty,
                    $"Cell ({x},{y}) is still empty after full cycle");
    }
}
