using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class DamageResolverTests
{
    private Board _board;

    [SetUp]
    public void SetUp()
    {
        _board = new Board(8, 8);
    }

    // ==========================================
    // CanDamage — Rule Table Verification
    // ==========================================

    [Test]
    public void CanDamage_Box_Blast_True()
    {
        var box = ItemFactory.CreateItem(ItemIds.Box);
        Assert.IsTrue(DamageResolver.CanDamage(box, DamageSource.Blast));
    }

    [Test]
    public void CanDamage_Box_Rocket_True()
    {
        var box = ItemFactory.CreateItem(ItemIds.Box);
        Assert.IsTrue(DamageResolver.CanDamage(box, DamageSource.Rocket));
    }

    [Test]
    public void CanDamage_Stone_Blast_False()
    {
        var stone = ItemFactory.CreateItem(ItemIds.Stone);
        Assert.IsFalse(DamageResolver.CanDamage(stone, DamageSource.Blast));
    }

    [Test]
    public void CanDamage_Stone_Rocket_True()
    {
        var stone = ItemFactory.CreateItem(ItemIds.Stone);
        Assert.IsTrue(DamageResolver.CanDamage(stone, DamageSource.Rocket));
    }

    [Test]
    public void CanDamage_Stone_Combo_True()
    {
        var stone = ItemFactory.CreateItem(ItemIds.Stone);
        Assert.IsTrue(DamageResolver.CanDamage(stone, DamageSource.Combo));
    }

    [Test]
    public void CanDamage_Vase_Blast_True()
    {
        var vase = ItemFactory.CreateItem(ItemIds.Vase);
        Assert.IsTrue(DamageResolver.CanDamage(vase, DamageSource.Blast));
    }

    [Test]
    public void CanDamage_Vase_Rocket_True()
    {
        var vase = ItemFactory.CreateItem(ItemIds.Vase);
        Assert.IsTrue(DamageResolver.CanDamage(vase, DamageSource.Rocket));
    }

    [Test]
    public void CanDamage_EmptyItem_ReturnsFalse()
    {
        var empty = ItemFactory.CreateEmpty();
        Assert.IsFalse(DamageResolver.CanDamage(empty, DamageSource.Blast));
        Assert.IsFalse(DamageResolver.CanDamage(empty, DamageSource.Rocket));
    }

    [Test]
    public void CanDamage_Cube_ReturnsFalse()
    {
        var cube = ItemFactory.CreateItem(ItemIds.Red);
        Assert.IsFalse(DamageResolver.CanDamage(cube, DamageSource.Blast));
    }

    [Test]
    public void CanDamage_DeadObstacle_ReturnsFalse()
    {
        var box = ItemFactory.CreateItem(ItemIds.Box);
        box.TakeDamage(1); // Kill it
        Assert.IsFalse(DamageResolver.CanDamage(box, DamageSource.Blast));
    }

    [Test]
    public void CanDamage_NullItem_ReturnsFalse()
    {
        Assert.IsFalse(DamageResolver.CanDamage(null, DamageSource.Blast));
    }

    // ==========================================
    // TryDamage — Applying Damage
    // ==========================================

    [Test]
    public void TryDamage_Box_Blast_DestroysIt()
    {
        var box = ItemFactory.CreateItem(ItemIds.Box);
        var result = DamageResolver.TryDamage(box, DamageSource.Blast);

        Assert.IsTrue(result.WasApplied);
        Assert.IsTrue(result.WasDestroyed);
        Assert.AreEqual(0, result.RemainingHealth);
    }

    [Test]
    public void TryDamage_Stone_Blast_Immune()
    {
        var stone = ItemFactory.CreateItem(ItemIds.Stone);
        var result = DamageResolver.TryDamage(stone, DamageSource.Blast);

        Assert.IsFalse(result.WasApplied);
        Assert.IsFalse(result.WasDestroyed);
        Assert.AreEqual(1, stone.Health); // Unchanged
    }

    [Test]
    public void TryDamage_Stone_Rocket_DestroysIt()
    {
        var stone = ItemFactory.CreateItem(ItemIds.Stone);
        var result = DamageResolver.TryDamage(stone, DamageSource.Rocket);

        Assert.IsTrue(result.WasApplied);
        Assert.IsTrue(result.WasDestroyed);
    }

    [Test]
    public void TryDamage_Vase_Blast_DamagesNotDestroys()
    {
        var vase = ItemFactory.CreateItem(ItemIds.Vase);
        var result = DamageResolver.TryDamage(vase, DamageSource.Blast);

        Assert.IsTrue(result.WasApplied);
        Assert.IsFalse(result.WasDestroyed);
        Assert.AreEqual(1, result.RemainingHealth);
    }

    [Test]
    public void TryDamage_Vase_TwoHits_DestroysIt()
    {
        var vase = ItemFactory.CreateItem(ItemIds.Vase);
        DamageResolver.TryDamage(vase, DamageSource.Blast);
        var result = DamageResolver.TryDamage(vase, DamageSource.Rocket);

        Assert.IsTrue(result.WasDestroyed);
        Assert.AreEqual(0, result.RemainingHealth);
    }

    // ==========================================
    // ProcessAdjacentDamage — Integration
    // ==========================================

    [Test]
    public void ProcessAdjacentDamage_BoxNextToBlast_Destroyed()
    {
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var blastCells = new List<Coordinate> { new Coordinate(1, 0) };
        var result = new BlastResult();

        DamageResolver.ProcessAdjacentDamage(_board, blastCells, DamageSource.Blast, result);

        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty);
        Assert.AreEqual(1, result.DestroyedObstacles.Count);
        Assert.AreEqual(1, result.DestroyedObstacleInfos.Count);
        Assert.AreEqual(ItemIds.Box, result.DestroyedObstacleInfos[0].ObstacleId);
    }

    [Test]
    public void ProcessAdjacentDamage_StoneNextToBlast_Untouched()
    {
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Stone));

        var blastCells = new List<Coordinate> { new Coordinate(1, 0) };
        var result = new BlastResult();

        DamageResolver.ProcessAdjacentDamage(_board, blastCells, DamageSource.Blast, result);

        Assert.AreEqual(ItemIds.Stone, _board.GetItem(2, 0).Id);
        Assert.AreEqual(1, _board.GetItem(2, 0).Health);
        Assert.AreEqual(0, result.DamagedObstacles.Count);
        Assert.AreEqual(0, result.DestroyedObstacles.Count);
    }

    [Test]
    public void ProcessAdjacentDamage_VaseOnlyTakesOneDamagePerBlast()
    {
        // Vase at (2,1) adjacent to TWO blast cells: (1,1) and (2,0)
        _board.SetItem(2, 1, ItemFactory.CreateItem(ItemIds.Vase));

        var blastCells = new List<Coordinate>
        {
            new Coordinate(1, 1), // left of vase
            new Coordinate(2, 0)  // below vase
        };
        var result = new BlastResult();

        DamageResolver.ProcessAdjacentDamage(_board, blastCells, DamageSource.Blast, result);

        // Vase should only take 1 damage despite being adjacent to 2 blast cells
        Assert.AreEqual(1, _board.GetItem(2, 1).Health);
        Assert.AreEqual(1, result.DamagedObstacles.Count);
        Assert.AreEqual(0, result.DestroyedObstacles.Count);
    }

    [Test]
    public void ProcessAdjacentDamage_MultipleBoxesDestroyedOneBlast()
    {
        // Two boxes flanking a blast cell
        _board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Box));
        _board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var blastCells = new List<Coordinate> { new Coordinate(1, 0) };
        var result = new BlastResult();

        DamageResolver.ProcessAdjacentDamage(_board, blastCells, DamageSource.Blast, result);

        Assert.IsTrue(_board.GetItem(0, 0).IsEmpty);
        Assert.IsTrue(_board.GetItem(2, 0).IsEmpty);
        Assert.AreEqual(2, result.DestroyedObstacles.Count);
    }

    [Test]
    public void ProcessAdjacentDamage_ObstacleFarFromBlast_Unharmed()
    {
        _board.SetItem(7, 7, ItemFactory.CreateItem(ItemIds.Box));

        var blastCells = new List<Coordinate> { new Coordinate(0, 0) };
        var result = new BlastResult();

        DamageResolver.ProcessAdjacentDamage(_board, blastCells, DamageSource.Blast, result);

        Assert.AreEqual(1, _board.GetItem(7, 7).Health);
    }

    // ==========================================
    // DamageAt — Direct Cell Damage (for Rockets)
    // ==========================================

    [Test]
    public void DamageAt_Box_Rocket_Destroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Box));
        var result = new BlastResult();

        var dmg = DamageResolver.DamageAt(_board, new Coordinate(3, 3), DamageSource.Rocket, result);

        Assert.IsTrue(dmg.WasDestroyed);
        Assert.IsTrue(_board.GetItem(3, 3).IsEmpty);
        Assert.AreEqual(1, result.DestroyedObstacleInfos.Count);
    }

    [Test]
    public void DamageAt_Stone_Rocket_Destroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Stone));
        var result = new BlastResult();

        var dmg = DamageResolver.DamageAt(_board, new Coordinate(3, 3), DamageSource.Rocket, result);

        Assert.IsTrue(dmg.WasDestroyed);
        Assert.IsTrue(_board.GetItem(3, 3).IsEmpty);
    }

    [Test]
    public void DamageAt_Vase_Rocket_DamagesNotDestroys()
    {
        _board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.Vase));
        var result = new BlastResult();

        var dmg = DamageResolver.DamageAt(_board, new Coordinate(3, 3), DamageSource.Rocket, result);

        Assert.IsTrue(dmg.WasApplied);
        Assert.IsFalse(dmg.WasDestroyed);
        Assert.AreEqual(1, _board.GetItem(3, 3).Health);
        Assert.AreEqual(1, result.DamagedObstacles.Count);
    }

    [Test]
    public void DamageAt_EmptyCell_NoDamage()
    {
        var result = new BlastResult();
        var dmg = DamageResolver.DamageAt(_board, new Coordinate(0, 0), DamageSource.Rocket, result);

        Assert.IsFalse(dmg.WasApplied);
        Assert.AreEqual(0, result.DamagedObstacles.Count);
        Assert.AreEqual(0, result.DestroyedObstacles.Count);
    }
}
