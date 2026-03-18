using NUnit.Framework;

[TestFixture]
public class ItemFactoryTests
{
    // --- Cube creation ---

    [TestCase(ItemIds.Red)]
    [TestCase(ItemIds.Green)]
    [TestCase(ItemIds.Blue)]
    [TestCase(ItemIds.Yellow)]
    public void CreateItem_Cube_CorrectProperties(string id)
    {
        var item = ItemFactory.CreateItem(id);

        Assert.AreEqual(id, item.Id);
        Assert.IsFalse(item.IsObstacle);
        Assert.IsTrue(item.IsMovable);
        Assert.AreEqual(1, item.Health);
        Assert.IsTrue(item.IsCube);
    }

    // --- Obstacle creation ---

    [Test]
    public void CreateItem_Box_CorrectProperties()
    {
        var item = ItemFactory.CreateItem("bo");

        Assert.AreEqual(ItemIds.Box, item.Id);
        Assert.IsTrue(item.IsObstacle);
        Assert.IsFalse(item.IsMovable);
        Assert.AreEqual(1, item.Health);
    }

    [Test]
    public void CreateItem_Stone_CorrectProperties()
    {
        var item = ItemFactory.CreateItem("s");

        Assert.AreEqual(ItemIds.Stone, item.Id);
        Assert.IsTrue(item.IsObstacle);
        Assert.IsFalse(item.IsMovable);
        Assert.AreEqual(1, item.Health);
    }

    [Test]
    public void CreateItem_Vase_CorrectProperties()
    {
        var item = ItemFactory.CreateItem("v");

        Assert.AreEqual(ItemIds.Vase, item.Id);
        Assert.IsTrue(item.IsObstacle);
        Assert.IsTrue(item.IsMovable);
        Assert.AreEqual(2, item.Health);
    }

    // --- Rocket creation ---

    [Test]
    public void CreateItem_VerticalRocket_CorrectProperties()
    {
        var item = ItemFactory.CreateItem("vro");

        Assert.AreEqual(ItemIds.VerticalRocket, item.Id);
        Assert.IsFalse(item.IsObstacle);
        Assert.IsTrue(item.IsMovable);
        Assert.AreEqual(1, item.Health);
    }

    [Test]
    public void CreateItem_HorizontalRocket_CorrectProperties()
    {
        var item = ItemFactory.CreateItem("hro");

        Assert.AreEqual(ItemIds.HorizontalRocket, item.Id);
        Assert.IsFalse(item.IsObstacle);
        Assert.IsTrue(item.IsMovable);
        Assert.AreEqual(1, item.Health);
    }

    // --- Random ---

    [Test]
    public void CreateItem_Rand_ProducesValidCube()
    {
        for (int i = 0; i < 50; i++)
        {
            var item = ItemFactory.CreateItem("rand");
            Assert.IsTrue(item.IsCube, $"Expected cube, got: {item.Id}");
            Assert.IsFalse(item.IsObstacle);
            Assert.IsTrue(item.IsMovable);
        }
    }

    [Test]
    public void CreateRandomCube_NeverProducesObstacleOrEmpty()
    {
        for (int i = 0; i < 100; i++)
        {
            var item = ItemFactory.CreateRandomCube();
            Assert.IsTrue(item.IsCube, $"Expected cube, got: {item.Id}");
            Assert.IsFalse(item.IsEmpty);
            Assert.IsFalse(item.IsObstacle);
        }
    }

    // --- Edge cases ---

    [Test]
    public void CreateEmpty_ReturnsCorrectDefaults()
    {
        var item = ItemFactory.CreateEmpty();

        Assert.AreEqual(ItemIds.None, item.Id);
        Assert.IsTrue(item.IsEmpty);
        Assert.AreEqual(0, item.Health);
        Assert.IsFalse(item.IsMovable);
    }

    [Test]
    public void CreateItem_UnknownId_ReturnsEmpty()
    {
        var item = ItemFactory.CreateItem("xyz_garbage");
        Assert.IsTrue(item.IsEmpty);
    }
}
