using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class RocketProcessorTests
{
    private Board _board;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
    }

    // ==========================================
    // QualifiesForRocket
    // ==========================================

    [Test]
    public void QualifiesForRocket_CountBelow4_False()
    {
        var processor = new RocketProcessor();
        Assert.IsFalse(processor.QualifiesForRocket(2));
        Assert.IsFalse(processor.QualifiesForRocket(3));
    }

    [Test]
    public void QualifiesForRocket_CountEquals4_True()
    {
        var processor = new RocketProcessor();
        Assert.IsTrue(processor.QualifiesForRocket(4));
    }

    [Test]
    public void QualifiesForRocket_CountAbove4_True()
    {
        var processor = new RocketProcessor();
        Assert.IsTrue(processor.QualifiesForRocket(5));
        Assert.IsTrue(processor.QualifiesForRocket(10));
    }

    // ==========================================
    // CreateRocket — Basic
    // ==========================================

    [Test]
    public void CreateRocket_GroupLT4_ReturnsNull()
    {
        var processor = new RocketProcessor();
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0)
        };

        var result = processor.CreateRocket(_board, new Coordinate(0, 0), matches);
        Assert.IsNull(result);
    }

    [Test]
    public void CreateRocket_GroupGTE4_ReturnsData()
    {
        var processor = new RocketProcessor();
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0)
        };

        var result = processor.CreateRocket(_board, new Coordinate(1, 0), matches);

        Assert.IsNotNull(result);
        Assert.AreEqual(new Coordinate(1, 0), result.SpawnPosition);
        Assert.AreEqual(4, result.SourceCoordinates.Count);
        Assert.IsTrue(
            result.RocketId == ItemIds.VerticalRocket ||
            result.RocketId == ItemIds.HorizontalRocket
        );
    }

    [Test]
    public void CreateRocket_PlacesRocketOnBoard()
    {
        var processor = new RocketProcessor();
        var coord = new Coordinate(3, 3);
        var matches = new List<Coordinate>
        {
            new Coordinate(2, 3),
            new Coordinate(3, 3),
            new Coordinate(4, 3),
            new Coordinate(5, 3)
        };

        var result = processor.CreateRocket(_board, coord, matches);

        // Board should now have a rocket at (3,3)
        GridItem boardItem = _board.GetItem(coord);
        Assert.IsTrue(boardItem.IsRocket);
        Assert.AreEqual(result.RocketId, boardItem.Id);
    }

    [Test]
    public void CreateRocket_RocketIsMovable()
    {
        var processor = new RocketProcessor();
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0)
        };

        processor.CreateRocket(_board, new Coordinate(0, 0), matches);

        GridItem rocket = _board.GetItem(0, 0);
        Assert.IsTrue(rocket.IsMovable, "Rockets should fall (IsMovable = true)");
        Assert.IsFalse(rocket.IsObstacle);
    }

    // ==========================================
    // CreateRocket — Randomness
    // ==========================================

    [Test]
    public void CreateRocket_ProducesBothDirections()
    {
        // With enough iterations, both directions should appear
        HashSet<string> directions = new HashSet<string>();
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0)
        };

        for (int i = 0; i < 50; i++)
        {
            var board = new Board(8, 8);
            var processor = new RocketProcessor();
            var result = processor.CreateRocket(board, new Coordinate(0, 0), matches);
            directions.Add(result.RocketId);

            if (directions.Count == 2)
                break; // Both found, test passes
        }

        Assert.AreEqual(2, directions.Count,
            "Expected both horizontal and vertical rockets to appear over 50 trials");
        CollectionAssert.Contains(directions, ItemIds.VerticalRocket);
        CollectionAssert.Contains(directions, ItemIds.HorizontalRocket);
    }

    [Test]
    public void CreateRocket_DeterministicWithSeed()
    {
        var matches = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(1, 0),
            new Coordinate(2, 0),
            new Coordinate(3, 0)
        };

        // Same seed should produce same result
        var processor1 = new RocketProcessor(42);
        var result1 = processor1.CreateRocket(new Board(8, 8), new Coordinate(0, 0), matches);

        var processor2 = new RocketProcessor(42);
        var result2 = processor2.CreateRocket(new Board(8, 8), new Coordinate(0, 0), matches);

        Assert.AreEqual(result1.RocketId, result2.RocketId);
    }

    // ==========================================
    // CreateRocket — SpawnPosition
    // ==========================================

    [Test]
    public void CreateRocket_SpawnPositionIsTappedCell()
    {
        var processor = new RocketProcessor();
        var tapped = new Coordinate(4, 5);
        var matches = new List<Coordinate>
        {
            new Coordinate(3, 5),
            new Coordinate(4, 5),
            new Coordinate(5, 5),
            new Coordinate(4, 6)
        };

        var result = processor.CreateRocket(_board, tapped, matches);

        Assert.AreEqual(tapped, result.SpawnPosition);
    }
}
