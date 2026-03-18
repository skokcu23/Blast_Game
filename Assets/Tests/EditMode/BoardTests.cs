using NUnit.Framework;

[TestFixture]
public class BoardTests
{
    private Board _board;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
    }

    // --- Initialization ---

    [Test]
    public void Constructor_SetsWidthAndHeight()
    {
        Assert.AreEqual(8, _board.Width);
        Assert.AreEqual(8, _board.Height);
    }

    [Test]
    public void Constructor_AllCellsStartEmpty()
    {
        for (int x = 0; x < _board.Width; x++)
            for (int y = 0; y < _board.Height; y++)
                Assert.IsTrue(_board.GetItem(x, y).IsEmpty);
    }

    [Test]
    public void InitializeRandom_NoCellsAreEmpty()
    {
        _board.InitializeRandom();

        for (int x = 0; x < _board.Width; x++)
            for (int y = 0; y < _board.Height; y++)
            {
                var item = _board.GetItem(x, y);
                Assert.IsFalse(item.IsEmpty, $"Cell ({x},{y}) is empty after random init");
                Assert.IsTrue(item.IsCube, $"Cell ({x},{y}) is not a cube: {item.Id}");
            }
    }

    [Test]
    public void Initialize_FromLevelData_PlacesItemsCorrectly()
    {
        // Create a minimal 6x6 level with a box at position (1,0) — bottom row, second cell
        var level = new LevelData
        {
            level_number = 99,
            grid_width = 6,
            grid_height = 6,
            move_count = 10,
            grid = new string[]
            {
                // Row 0 (bottom)
                "r", "bo", "g", "b", "y", "r",
                // Row 1
                "rand", "rand", "rand", "rand", "rand", "rand",
                // Row 2
                "rand", "rand", "s", "rand", "rand", "rand",
                // Row 3
                "rand", "rand", "rand", "v", "rand", "rand",
                // Row 4
                "rand", "rand", "rand", "rand", "rand", "rand",
                // Row 5 (top)
                "rand", "rand", "rand", "rand", "rand", "rand",
            }
        };

        var board = new Board(6, 6);
        board.Initialize(level);

        // Check specific placements
        Assert.AreEqual(ItemIds.Red, board.GetItem(0, 0).Id);
        Assert.AreEqual(ItemIds.Box, board.GetItem(1, 0).Id);
        Assert.IsTrue(board.GetItem(1, 0).IsObstacle);
        Assert.AreEqual(ItemIds.Green, board.GetItem(2, 0).Id);
        Assert.AreEqual(ItemIds.Stone, board.GetItem(2, 2).Id);
        Assert.AreEqual(ItemIds.Vase, board.GetItem(3, 3).Id);
        Assert.AreEqual(2, board.GetItem(3, 3).Health); // Vase has 2 HP
    }

    // --- GetItem / SetItem ---

    [Test]
    public void SetItem_GetItem_RoundTrip()
    {
        var item = ItemFactory.CreateItem(ItemIds.Red);
        var coord = new Coordinate(3, 4);

        _board.SetItem(coord, item);
        var retrieved = _board.GetItem(coord);

        Assert.AreEqual(ItemIds.Red, retrieved.Id);
    }

    [Test]
    public void GetItem_OutOfBounds_ReturnsEmpty()
    {
        var item = _board.GetItem(-1, 0);
        Assert.IsTrue(item.IsEmpty);

        item = _board.GetItem(100, 100);
        Assert.IsTrue(item.IsEmpty);
    }

    [Test]
    public void SetItem_OutOfBounds_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            _board.SetItem(-1, 0, ItemFactory.CreateItem(ItemIds.Red));
            _board.SetItem(100, 100, ItemFactory.CreateItem(ItemIds.Red));
        });
    }

    // --- Coordinate validation ---

    [Test]
    public void IsValidCoordinate_InsideBounds_ReturnsTrue()
    {
        Assert.IsTrue(_board.IsValidCoordinate(0, 0));
        Assert.IsTrue(_board.IsValidCoordinate(7, 7));
        Assert.IsTrue(_board.IsValidCoordinate(4, 3));
    }

    [Test]
    public void IsValidCoordinate_OutsideBounds_ReturnsFalse()
    {
        Assert.IsFalse(_board.IsValidCoordinate(-1, 0));
        Assert.IsFalse(_board.IsValidCoordinate(0, -1));
        Assert.IsFalse(_board.IsValidCoordinate(8, 0));
        Assert.IsFalse(_board.IsValidCoordinate(0, 8));
    }

    // --- Obstacle queries ---

    [Test]
    public void AreAllObstaclesCleared_NoObstacles_ReturnsTrue()
    {
        _board.InitializeRandom(); // Only cubes
        Assert.IsTrue(_board.AreAllObstaclesCleared());
    }

    [Test]
    public void AreAllObstaclesCleared_WithLiveObstacle_ReturnsFalse()
    {
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Box));
        Assert.IsFalse(_board.AreAllObstaclesCleared());
    }

    [Test]
    public void AreAllObstaclesCleared_AfterDestroyingAllObstacles_ReturnsTrue()
    {
        var box = ItemFactory.CreateItem(ItemIds.Box);
        _board.SetItem(2, 2, box);

        // Simulate destroying it
        box.TakeDamage(1);
        _board.SetItem(2, 2, ItemFactory.CreateEmpty());

        Assert.IsTrue(_board.AreAllObstaclesCleared());
    }

    [Test]
    public void CountObstacles_ReturnsCorrectCount()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Vase));
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Stone));

        Assert.AreEqual(4, _board.CountObstacles());
        Assert.AreEqual(2, _board.CountObstacles(ItemIds.Box));
        Assert.AreEqual(1, _board.CountObstacles(ItemIds.Vase));
        Assert.AreEqual(1, _board.CountObstacles(ItemIds.Stone));
    }

    [Test]
    public void CountObstacles_FilterById_IgnoresOtherTypes()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Vase));

        Assert.AreEqual(1, _board.CountObstacles(ItemIds.Box));
        Assert.AreEqual(0, _board.CountObstacles(ItemIds.Stone));
    }
}
