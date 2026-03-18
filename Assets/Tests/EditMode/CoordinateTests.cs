using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class CoordinateTests
{
    [Test]
    public void Constructor_SetsValues()
    {
        var coord = new Coordinate(3, 7);
        Assert.AreEqual(3, coord.x);
        Assert.AreEqual(7, coord.y);
    }

    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Coordinate(2, 5);
        var b = new Coordinate(2, 5);
        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new Coordinate(2, 5);
        var b = new Coordinate(3, 5);
        Assert.IsFalse(a.Equals(b));
        Assert.IsTrue(a != b);
    }

    [Test]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new Coordinate(4, 6);
        var b = new Coordinate(4, 6);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void GetHashCode_DifferentValues_DifferentHash()
    {
        var a = new Coordinate(1, 2);
        var b = new Coordinate(2, 1);
        Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void DictionaryKey_WorksCorrectly()
    {
        var dict = new Dictionary<Coordinate, string>();
        var coord = new Coordinate(3, 4);
        dict[coord] = "hello";

        var lookup = new Coordinate(3, 4);
        Assert.IsTrue(dict.ContainsKey(lookup));
        Assert.AreEqual("hello", dict[lookup]);
    }

    [Test]
    public void DictionaryKey_RemoveWorks()
    {
        var dict = new Dictionary<Coordinate, int>();
        dict[new Coordinate(1, 1)] = 10;
        dict[new Coordinate(2, 2)] = 20;

        dict.Remove(new Coordinate(1, 1));

        Assert.IsFalse(dict.ContainsKey(new Coordinate(1, 1)));
        Assert.IsTrue(dict.ContainsKey(new Coordinate(2, 2)));
    }

    [Test]
    public void ToString_FormatsCorrectly()
    {
        var coord = new Coordinate(5, 9);
        Assert.AreEqual("(5, 9)", coord.ToString());
    }
}
