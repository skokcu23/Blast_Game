using NUnit.Framework;

[TestFixture]
public class LevelDataTests
{
    // --- Validation ---

    [Test]
    public void IsValid_CorrectData_ReturnsTrue()
    {
        var data = CreateValidLevel(8, 8, 20);
        Assert.IsTrue(data.IsValid());
    }

    [Test]
    public void IsValid_WidthTooSmall_ReturnsFalse()
    {
        var data = CreateValidLevel(5, 8, 20); // min is 6
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_WidthTooLarge_ReturnsFalse()
    {
        var data = CreateValidLevel(11, 8, 20); // max is 10
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_HeightTooSmall_ReturnsFalse()
    {
        var data = CreateValidLevel(8, 5, 20);
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_HeightTooLarge_ReturnsFalse()
    {
        var data = CreateValidLevel(8, 11, 20);
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_ZeroMoves_ReturnsFalse()
    {
        var data = CreateValidLevel(8, 8, 0);
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_NullGrid_ReturnsFalse()
    {
        var data = new LevelData
        {
            level_number = 1,
            grid_width = 8,
            grid_height = 8,
            move_count = 20,
            grid = null
        };
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_WrongGridLength_ReturnsFalse()
    {
        var data = new LevelData
        {
            level_number = 1,
            grid_width = 8,
            grid_height = 8,
            move_count = 20,
            grid = new string[10] // Should be 64
        };
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void IsValid_MinBounds_ReturnsTrue()
    {
        var data = CreateValidLevel(6, 6, 1);
        Assert.IsTrue(data.IsValid());
    }

    [Test]
    public void IsValid_MaxBounds_ReturnsTrue()
    {
        var data = CreateValidLevel(10, 10, 99);
        Assert.IsTrue(data.IsValid());
    }

    // --- Grid Indexing ---

    [Test]
    public void GetItemIdAt_BottomLeftIsFirstElement()
    {
        var data = new LevelData
        {
            level_number = 1,
            grid_width = 6,
            grid_height = 6,
            move_count = 10,
            grid = new string[36]
        };

        data.grid[0] = "bo"; // (0,0) = bottom-left

        Assert.AreEqual("bo", data.GetItemIdAt(0, 0));
    }

    [Test]
    public void GetItemIdAt_SecondRowStartsAtWidth()
    {
        var data = new LevelData
        {
            level_number = 1,
            grid_width = 8,
            grid_height = 8,
            move_count = 10,
            grid = new string[64]
        };

        // Row 1 starts at index 8 (grid_width)
        data.grid[8] = "s";

        Assert.AreEqual("s", data.GetItemIdAt(0, 1));
    }

    [Test]
    public void GetItemIdAt_TopRightIsLastElement()
    {
        var data = CreateValidLevel(8, 8, 10);
        data.grid[63] = "v"; // Last element = (7, 7) = top-right

        Assert.AreEqual("v", data.GetItemIdAt(7, 7));
    }

    [Test]
    public void GetItemIdAt_OutOfBounds_ReturnsNone()
    {
        var data = CreateValidLevel(8, 8, 10);

        Assert.AreEqual(ItemIds.None, data.GetItemIdAt(-1, 0));
        Assert.AreEqual(ItemIds.None, data.GetItemIdAt(0, -1));
        Assert.AreEqual(ItemIds.None, data.GetItemIdAt(100, 0));
    }

    [Test]
    public void GetItemIdAt_SpecificCoordinates()
    {
        // 6x6 grid, place specific items at known positions
        var data = new LevelData
        {
            level_number = 1,
            grid_width = 6,
            grid_height = 6,
            move_count = 10,
            grid = new string[36]
        };

        // Fill with rand
        for (int i = 0; i < 36; i++)
            data.grid[i] = "rand";

        // Place specific items:
        // (2, 3) = index 3*6 + 2 = 20
        data.grid[20] = "bo";
        // (5, 5) = index 5*6 + 5 = 35
        data.grid[35] = "v";

        Assert.AreEqual("bo", data.GetItemIdAt(2, 3));
        Assert.AreEqual("v", data.GetItemIdAt(5, 5));
    }

    // --- Helper ---

    private LevelData CreateValidLevel(int width, int height, int moves)
    {
        var data = new LevelData
        {
            level_number = 1,
            grid_width = width,
            grid_height = height,
            move_count = moves,
            grid = new string[width * height]
        };

        for (int i = 0; i < data.grid.Length; i++)
            data.grid[i] = "rand";

        return data;
    }
}
