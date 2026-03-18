using NUnit.Framework;

[TestFixture]
public class ObstacleGoalTrackerTests
{
    private Board _board;
    private ObstacleGoalTracker _tracker;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
        _tracker = new ObstacleGoalTracker();
    }

    // ==========================================
    // Initialization
    // ==========================================

    [Test]
    public void Initialize_EmptyBoard_ZeroGoals()
    {
        _tracker.InitializeFromBoard(_board);

        Assert.IsTrue(_tracker.AreAllGoalsMet());
        Assert.AreEqual(0, _tracker.GetTotalRemaining());
    }

    [Test]
    public void Initialize_WithBoxes_CountsCorrectly()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 1, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(2, 2, ItemFactory.CreateItem(ItemIds.Box));

        _tracker.InitializeFromBoard(_board);

        Assert.AreEqual(3, _tracker.GetRemainingCount(ItemIds.Box));
        Assert.AreEqual(3, _tracker.GetInitialCount(ItemIds.Box));
        Assert.AreEqual(3, _tracker.GetTotalRemaining());
        Assert.IsFalse(_tracker.AreAllGoalsMet());
    }

    [Test]
    public void Initialize_MixedObstacles_CountsPerType()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Stone));
        _board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.Vase));
        _board.SetItem(4, 0, ItemFactory.CreateItem(ItemIds.Vase));

        _tracker.InitializeFromBoard(_board);

        Assert.AreEqual(2, _tracker.GetRemainingCount(ItemIds.Box));
        Assert.AreEqual(1, _tracker.GetRemainingCount(ItemIds.Stone));
        Assert.AreEqual(2, _tracker.GetRemainingCount(ItemIds.Vase));
        Assert.AreEqual(5, _tracker.GetTotalRemaining());
    }

    [Test]
    public void Initialize_IgnoresCubes()
    {
        _board.InitializeRandom(); // Fill with cubes only
        _tracker.InitializeFromBoard(_board);

        Assert.AreEqual(0, _tracker.GetTotalRemaining());
        Assert.IsTrue(_tracker.AreAllGoalsMet());
    }

    // ==========================================
    // Destruction Tracking
    // ==========================================

    [Test]
    public void OnObstacleDestroyed_DecrementsCount()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Box));
        _tracker.InitializeFromBoard(_board);

        _tracker.OnObstacleDestroyed(ItemIds.Box);

        Assert.AreEqual(1, _tracker.GetRemainingCount(ItemIds.Box));
        Assert.AreEqual(2, _tracker.GetInitialCount(ItemIds.Box)); // Initial unchanged
    }

    [Test]
    public void OnObstacleDestroyed_AllCleared_GoalsMet()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _tracker.InitializeFromBoard(_board);

        _tracker.OnObstacleDestroyed(ItemIds.Box);

        Assert.IsTrue(_tracker.AreAllGoalsMet());
        Assert.AreEqual(0, _tracker.GetTotalRemaining());
    }

    [Test]
    public void OnObstacleDestroyed_MixedTypes_AllMustBeCleared()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Stone));
        _tracker.InitializeFromBoard(_board);

        _tracker.OnObstacleDestroyed(ItemIds.Box);

        Assert.IsFalse(_tracker.AreAllGoalsMet()); // Stone still alive
        Assert.AreEqual(1, _tracker.GetTotalRemaining());

        _tracker.OnObstacleDestroyed(ItemIds.Stone);

        Assert.IsTrue(_tracker.AreAllGoalsMet());
    }

    [Test]
    public void OnObstacleDestroyed_UnknownType_DoesNotCrash()
    {
        _tracker.InitializeFromBoard(_board);

        Assert.DoesNotThrow(() => _tracker.OnObstacleDestroyed("nonexistent"));
    }

    [Test]
    public void OnObstacleDestroyed_CannotGoNegative()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _tracker.InitializeFromBoard(_board);

        _tracker.OnObstacleDestroyed(ItemIds.Box);
        _tracker.OnObstacleDestroyed(ItemIds.Box); // Extra call

        Assert.AreEqual(0, _tracker.GetRemainingCount(ItemIds.Box));
    }

    // ==========================================
    // Goal Types
    // ==========================================

    [Test]
    public void GetGoalTypes_ReturnsAllObstacleTypes()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Vase));
        _tracker.InitializeFromBoard(_board);

        var types = _tracker.GetGoalTypes();

        Assert.AreEqual(2, types.Count);
        CollectionAssert.Contains(types, ItemIds.Box);
        CollectionAssert.Contains(types, ItemIds.Vase);
    }

    [Test]
    public void GetRemainingCount_TypeNotInLevel_ReturnsZero()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _tracker.InitializeFromBoard(_board);

        Assert.AreEqual(0, _tracker.GetRemainingCount(ItemIds.Stone));
    }

    // ==========================================
    // Reinitialize (level restart)
    // ==========================================

    [Test]
    public void Initialize_CalledTwice_ResetsCompletely()
    {
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _tracker.InitializeFromBoard(_board);
        _tracker.OnObstacleDestroyed(ItemIds.Box);

        // Reinitialize with a fresh board
        var newBoard = new Board(8, 8);
        newBoard.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Vase));
        newBoard.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Vase));
        _tracker.InitializeFromBoard(newBoard);

        Assert.AreEqual(0, _tracker.GetRemainingCount(ItemIds.Box)); // Old type gone
        Assert.AreEqual(2, _tracker.GetRemainingCount(ItemIds.Vase));
        Assert.IsFalse(_tracker.AreAllGoalsMet());
    }
}
