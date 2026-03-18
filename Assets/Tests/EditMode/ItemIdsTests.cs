using NUnit.Framework;

[TestFixture]
public class ItemIdsTests
{
    [Test]
    public void IsCube_ColorIds_ReturnsTrue()
    {
        Assert.IsTrue(ItemIds.IsCube("r"));
        Assert.IsTrue(ItemIds.IsCube("g"));
        Assert.IsTrue(ItemIds.IsCube("b"));
        Assert.IsTrue(ItemIds.IsCube("y"));
    }

    [Test]
    public void IsCube_NonCubeIds_ReturnsFalse()
    {
        Assert.IsFalse(ItemIds.IsCube("bo"));
        Assert.IsFalse(ItemIds.IsCube("s"));
        Assert.IsFalse(ItemIds.IsCube("v"));
        Assert.IsFalse(ItemIds.IsCube("vro"));
        Assert.IsFalse(ItemIds.IsCube("none"));
    }

    [Test]
    public void IsRocket_RocketIds_ReturnsTrue()
    {
        Assert.IsTrue(ItemIds.IsRocket("vro"));
        Assert.IsTrue(ItemIds.IsRocket("hro"));
    }

    [Test]
    public void IsRocket_NonRocketIds_ReturnsFalse()
    {
        Assert.IsFalse(ItemIds.IsRocket("r"));
        Assert.IsFalse(ItemIds.IsRocket("bo"));
        Assert.IsFalse(ItemIds.IsRocket("none"));
    }

    [Test]
    public void IsObstacle_ObstacleIds_ReturnsTrue()
    {
        Assert.IsTrue(ItemIds.IsObstacle("bo"));
        Assert.IsTrue(ItemIds.IsObstacle("s"));
        Assert.IsTrue(ItemIds.IsObstacle("v"));
    }

    [Test]
    public void IsObstacle_NonObstacleIds_ReturnsFalse()
    {
        Assert.IsFalse(ItemIds.IsObstacle("r"));
        Assert.IsFalse(ItemIds.IsObstacle("vro"));
        Assert.IsFalse(ItemIds.IsObstacle("none"));
    }

    [Test]
    public void CubeIds_ContainsAllFourColors()
    {
        Assert.AreEqual(4, ItemIds.CubeIds.Length);
        CollectionAssert.Contains(ItemIds.CubeIds, "r");
        CollectionAssert.Contains(ItemIds.CubeIds, "g");
        CollectionAssert.Contains(ItemIds.CubeIds, "b");
        CollectionAssert.Contains(ItemIds.CubeIds, "y");
    }
}
