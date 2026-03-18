using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class TapResolverTests
{
    private Board _board;
    private TapResolver _resolver;
    private ClassicMatchStrategy _matchStrategy;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
        _resolver = new TapResolver();
        _matchStrategy = new ClassicMatchStrategy();
    }

    private void PlaceCube(int x, int y, string id)
    {
        _board.SetItem(x, y, ItemFactory.CreateItem(id));
    }

    // ==========================================
    // TapAction.None
    // ==========================================

    [Test]
    public void Resolve_EmptyCell_ReturnsNone()
    {
        var result = _resolver.Resolve(_board, new Coordinate(0, 0), _matchStrategy);
        Assert.AreEqual(TapAction.None, result.Action);
    }

    [Test]
    public void Resolve_Obstacle_ReturnsNone()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Box));
        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);
        Assert.AreEqual(TapAction.None, result.Action);
    }

    [Test]
    public void Resolve_SingleCubeNoMatch_ReturnsNone()
    {
        PlaceCube(3, 3, ItemIds.Red);
        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);
        Assert.AreEqual(TapAction.None, result.Action);
    }

    [Test]
    public void Resolve_OutOfBounds_ReturnsNone()
    {
        var result = _resolver.Resolve(_board, new Coordinate(-1, -1), _matchStrategy);
        Assert.AreEqual(TapAction.None, result.Action);
    }

    // ==========================================
    // TapAction.BlastGroup
    // ==========================================

    [Test]
    public void Resolve_TwoCubes_ReturnsBlastGroup()
    {
        PlaceCube(0, 0, ItemIds.Red);
        PlaceCube(1, 0, ItemIds.Red);

        var result = _resolver.Resolve(_board, new Coordinate(0, 0), _matchStrategy);

        Assert.AreEqual(TapAction.BlastGroup, result.Action);
        Assert.AreEqual(2, result.MatchedCoordinates.Count);
        Assert.AreEqual(new Coordinate(0, 0), result.TappedCoord);
    }

    [Test]
    public void Resolve_FiveCubes_ReturnsBlastGroup_WithAllMatches()
    {
        PlaceCube(0, 0, ItemIds.Blue);
        PlaceCube(1, 0, ItemIds.Blue);
        PlaceCube(2, 0, ItemIds.Blue);
        PlaceCube(3, 0, ItemIds.Blue);
        PlaceCube(4, 0, ItemIds.Blue);

        var result = _resolver.Resolve(_board, new Coordinate(2, 0), _matchStrategy);

        Assert.AreEqual(TapAction.BlastGroup, result.Action);
        Assert.AreEqual(5, result.MatchedCoordinates.Count);
    }

    [Test]
    public void Resolve_BlastGroup_StoresCorrectTappedCoord()
    {
        PlaceCube(3, 4, ItemIds.Green);
        PlaceCube(4, 4, ItemIds.Green);

        var result = _resolver.Resolve(_board, new Coordinate(4, 4), _matchStrategy);

        Assert.AreEqual(TapAction.BlastGroup, result.Action);
        Assert.AreEqual(new Coordinate(4, 4), result.TappedCoord);
    }

    // ==========================================
    // TapAction.ExplodeRocket
    // ==========================================

    [Test]
    public void Resolve_RocketAlone_ReturnsExplodeRocket()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action);
        Assert.AreEqual(new Coordinate(3, 3), result.TappedCoord);
    }

    [Test]
    public void Resolve_HorizontalRocketAlone_ReturnsExplodeRocket()
    {
        _board.SetItem(5, 5, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var result = _resolver.Resolve(_board, new Coordinate(5, 5), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action);
    }

    [Test]
    public void Resolve_RocketNextToCubes_StillExplodeRocket()
    {
        // Rockets don't join cube groups
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(3, 4, ItemIds.Red);
        PlaceCube(3, 2, ItemIds.Red);

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action);
    }

    // ==========================================
    // TapAction.RocketCombo
    // ==========================================

    [Test]
    public void Resolve_TwoAdjacentRockets_ReturnsCombo()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.RocketCombo, result.Action);
        Assert.AreEqual(new Coordinate(3, 3), result.TappedCoord);
        Assert.AreEqual(1, result.AdjacentRockets.Count);
        CollectionAssert.Contains(result.AdjacentRockets, new Coordinate(3, 4));
    }

    [Test]
    public void Resolve_RocketWithMultipleAdjacentRockets_ReturnsAllInCombo()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        _board.SetItem(2, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.RocketCombo, result.Action);
        Assert.AreEqual(2, result.AdjacentRockets.Count);
    }

    [Test]
    public void Resolve_DiagonalRockets_NoCombo()
    {
        // Diagonal is NOT adjacent
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(4, 4, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action); // No combo
    }

    [Test]
    public void Resolve_RocketNextToObstacle_NotCombo()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        _board.SetItem(3, 4, ItemFactory.CreateItem(ItemIds.Box));

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action); // Box is not a rocket
    }

    // ==========================================
    // PRIORITY: Rocket trumps cube match
    // ==========================================

    [Test]
    public void Resolve_RocketEvenIfAdjacentToMatchGroup_ReturnsRocketAction()
    {
        // Rocket at (3,3), cubes around it — tapping the rocket should explode, not match cubes
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        PlaceCube(2, 3, ItemIds.Red);
        PlaceCube(4, 3, ItemIds.Red);

        var result = _resolver.Resolve(_board, new Coordinate(3, 3), _matchStrategy);

        Assert.AreEqual(TapAction.ExplodeRocket, result.Action);
    }
}
