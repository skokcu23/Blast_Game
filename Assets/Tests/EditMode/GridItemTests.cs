using NUnit.Framework;

[TestFixture]
public class GridItemTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var item = new GridItem("bo", isObstacle: true, isMovable: false, health: 1);

        Assert.AreEqual("bo", item.Id);
        Assert.IsTrue(item.IsObstacle);
        Assert.IsFalse(item.IsMovable);
        Assert.AreEqual(1, item.Health);
    }

    [Test]
    public void IsEmpty_NoneId_ReturnsTrue()
    {
        var item = new GridItem(ItemIds.None, false, false, 0);
        Assert.IsTrue(item.IsEmpty);
    }

    [Test]
    public void IsEmpty_CubeId_ReturnsFalse()
    {
        var item = new GridItem(ItemIds.Red, false, true, 1);
        Assert.IsFalse(item.IsEmpty);
    }

    [Test]
    public void IsCube_ColorItem_ReturnsTrue()
    {
        var item = new GridItem(ItemIds.Blue, false, true, 1);
        Assert.IsTrue(item.IsCube);
    }

    [Test]
    public void IsRocket_RocketItem_ReturnsTrue()
    {
        var item = new GridItem(ItemIds.VerticalRocket, false, true, 1);
        Assert.IsTrue(item.IsRocket);
    }

    [Test]
    public void IsAlive_PositiveHealth_ReturnsTrue()
    {
        var item = new GridItem(ItemIds.Box, true, false, 1);
        Assert.IsTrue(item.IsAlive);
    }

    [Test]
    public void IsAlive_ZeroHealth_ReturnsFalse()
    {
        var item = new GridItem(ItemIds.None, false, false, 0);
        Assert.IsFalse(item.IsAlive);
    }

    [Test]
    public void TakeDamage_OneHP_ReturnsTrue_Destroyed()
    {
        var item = new GridItem(ItemIds.Box, true, false, 1);
        bool destroyed = item.TakeDamage(1);

        Assert.IsTrue(destroyed);
        Assert.AreEqual(0, item.Health);
        Assert.IsFalse(item.IsAlive);
    }

    [Test]
    public void TakeDamage_TwoHP_ReturnsFalse_StillAlive()
    {
        var item = new GridItem(ItemIds.Vase, true, true, 2);
        bool destroyed = item.TakeDamage(1);

        Assert.IsFalse(destroyed);
        Assert.AreEqual(1, item.Health);
        Assert.IsTrue(item.IsAlive);
    }

    [Test]
    public void TakeDamage_EmptyItem_ReturnsFalse()
    {
        var item = new GridItem(ItemIds.None, false, false, 0);
        bool destroyed = item.TakeDamage(1);
        Assert.IsFalse(destroyed);
    }

    [Test]
    public void TakeDamage_TwoHits_DestroysVase()
    {
        var item = new GridItem(ItemIds.Vase, true, true, 2);
        item.TakeDamage(1);
        bool destroyed = item.TakeDamage(1);

        Assert.IsTrue(destroyed);
        Assert.AreEqual(0, item.Health);
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new GridItem(ItemIds.Vase, true, true, 2);
        var clone = original.Clone();

        clone.TakeDamage(1);

        Assert.AreEqual(2, original.Health);
        Assert.AreEqual(1, clone.Health);
    }
}
