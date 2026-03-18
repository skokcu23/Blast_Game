using NUnit.Framework;

[TestFixture]
public class LevelProgressionManagerTests
{
    // ==========================================
    // BASIC STATE
    // ==========================================

    [Test]
    public void Constructor_StartsAtLevel1_WhenPersistenceReturns1()
    {
        var persistence = new InMemoryLevelPersistence(1);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual(1, manager.CurrentLevel);
        Assert.IsFalse(manager.AllLevelsComplete);
    }

    [Test]
    public void Constructor_RestoresFromPersistence()
    {
        var persistence = new InMemoryLevelPersistence(5);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual(5, manager.CurrentLevel);
    }

    [Test]
    public void Constructor_WithoutPersistence_StartsAt1()
    {
        var manager = new LevelProgressionManager(10);

        Assert.AreEqual(1, manager.CurrentLevel);
    }

    [Test]
    public void TotalLevels_ReturnsCorrectValue()
    {
        var manager = new LevelProgressionManager(10);
        Assert.AreEqual(10, manager.TotalLevels);
    }

    // ==========================================
    // ADVANCEMENT
    // ==========================================

    [Test]
    public void AdvanceLevel_IncrementsBy1()
    {
        var manager = new LevelProgressionManager(10);
        manager.AdvanceLevel();

        Assert.AreEqual(2, manager.CurrentLevel);
    }

    [Test]
    public void AdvanceLevel_PersistsNewLevel()
    {
        var persistence = new InMemoryLevelPersistence(1);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.AdvanceLevel();

        Assert.AreEqual(2, persistence.LoadCurrentLevel());
    }

    [Test]
    public void AdvanceLevel_MultipleAdvances()
    {
        var manager = new LevelProgressionManager(10);

        for (int i = 0; i < 5; i++)
            manager.AdvanceLevel();

        Assert.AreEqual(6, manager.CurrentLevel);
    }

    [Test]
    public void AdvanceLevel_ToLastLevel()
    {
        var persistence = new InMemoryLevelPersistence(9);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.AdvanceLevel(); // Now at 10

        Assert.AreEqual(10, manager.CurrentLevel);
        Assert.IsFalse(manager.AllLevelsComplete);
    }

    [Test]
    public void AdvanceLevel_PastLastLevel_AllComplete()
    {
        var persistence = new InMemoryLevelPersistence(10);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.AdvanceLevel(); // Now at 11 = all complete

        Assert.AreEqual(11, manager.CurrentLevel);
        Assert.IsTrue(manager.AllLevelsComplete);
    }

    [Test]
    public void AdvanceLevel_WhenAlreadyComplete_NoOp()
    {
        var persistence = new InMemoryLevelPersistence(11);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.AdvanceLevel(); // Should not go to 12

        Assert.AreEqual(11, manager.CurrentLevel);
    }

    // ==========================================
    // SET LEVEL (Editor menu item)
    // ==========================================

    [Test]
    public void SetLevel_SetsExactLevel()
    {
        var manager = new LevelProgressionManager(10);
        manager.SetLevel(7);

        Assert.AreEqual(7, manager.CurrentLevel);
    }

    [Test]
    public void SetLevel_PersistsNewLevel()
    {
        var persistence = new InMemoryLevelPersistence(1);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.SetLevel(3);

        Assert.AreEqual(3, persistence.LoadCurrentLevel());
    }

    [Test]
    public void SetLevel_ClampsBelow1()
    {
        var manager = new LevelProgressionManager(10);
        manager.SetLevel(0);

        Assert.AreEqual(1, manager.CurrentLevel);
    }

    [Test]
    public void SetLevel_ClampsNegative()
    {
        var manager = new LevelProgressionManager(10);
        manager.SetLevel(-5);

        Assert.AreEqual(1, manager.CurrentLevel);
    }

    [Test]
    public void SetLevel_ClampsAboveTotal()
    {
        var manager = new LevelProgressionManager(10);
        manager.SetLevel(99);

        Assert.AreEqual(11, manager.CurrentLevel); // totalLevels + 1 = "all complete"
    }

    [Test]
    public void SetLevel_CanSetToAllComplete()
    {
        var manager = new LevelProgressionManager(10);
        manager.SetLevel(11);

        Assert.IsTrue(manager.AllLevelsComplete);
    }

    [Test]
    public void SetLevel_CanResetToLevel1()
    {
        var persistence = new InMemoryLevelPersistence(8);
        var manager = new LevelProgressionManager(persistence, 10);

        manager.SetLevel(1);

        Assert.AreEqual(1, manager.CurrentLevel);
        Assert.IsFalse(manager.AllLevelsComplete);
    }

    // ==========================================
    // GET LEVEL TO PLAY
    // ==========================================

    [Test]
    public void GetLevelToPlay_ReturnsCurrentLevel()
    {
        var manager = new LevelProgressionManager(10);
        Assert.AreEqual(1, manager.GetLevelToPlay());
    }

    [Test]
    public void GetLevelToPlay_WhenComplete_ReturnsNeg1()
    {
        var persistence = new InMemoryLevelPersistence(11);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual(-1, manager.GetLevelToPlay());
    }

    // ==========================================
    // BUTTON TEXT
    // ==========================================

    [Test]
    public void GetLevelButtonText_ShowsLevelNumber()
    {
        var persistence = new InMemoryLevelPersistence(3);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual("Level 3", manager.GetLevelButtonText());
    }

    [Test]
    public void GetLevelButtonText_ShowsFinished_WhenComplete()
    {
        var persistence = new InMemoryLevelPersistence(11);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual("Finished", manager.GetLevelButtonText());
    }

    [Test]
    public void GetLevelButtonText_ShowsLevel10_BeforeFinished()
    {
        var persistence = new InMemoryLevelPersistence(10);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual("Level 10", manager.GetLevelButtonText());
    }

    // ==========================================
    // PERSISTENCE CLAMPING
    // ==========================================

    [Test]
    public void Constructor_ClampsCorruptPersistence_TooLow()
    {
        var persistence = new InMemoryLevelPersistence(0);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual(1, manager.CurrentLevel);
    }

    [Test]
    public void Constructor_ClampsCorruptPersistence_TooHigh()
    {
        var persistence = new InMemoryLevelPersistence(999);
        var manager = new LevelProgressionManager(persistence, 10);

        Assert.AreEqual(11, manager.CurrentLevel); // Clamped to totalLevels+1
    }
}
