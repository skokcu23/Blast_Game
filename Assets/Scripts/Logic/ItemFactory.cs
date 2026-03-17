using System;

/// <summary>
/// Factory Pattern: The single place that translates string IDs from level files
/// into fully configured GridItem objects.
///
/// PDF keys: r, g, b, y, rand, vro, hro, bo, s, v
///
/// Property rules (from Case Study):
///   Cubes:   movable, not obstacle, health 1
///   Rockets: movable, not obstacle, health 1
///   Box:     NOT movable, obstacle, health 1
///   Stone:   NOT movable, obstacle, health 1
///   Vase:    movable (falls!), obstacle, health 2
/// </summary>
public static class ItemFactory
{
    private static readonly Random _rand = new Random();

    /// <summary>
    /// Create a GridItem from a level-file string key.
    /// Returns an empty item for unrecognized keys (fail gracefully).
    /// </summary>
    public static GridItem CreateItem(string id)
    {
        // Handle "rand" by picking a random cube color
        if (id == ItemIds.Random)
        {
            id = ItemIds.CubeIds[_rand.Next(0, ItemIds.CubeIds.Length)];
        }

        return id switch
        {
            // Cubes: movable, not obstacle, 1 hp
            ItemIds.Red    => new GridItem(ItemIds.Red,    isObstacle: false, isMovable: true, health: 1),
            ItemIds.Green  => new GridItem(ItemIds.Green,  isObstacle: false, isMovable: true, health: 1),
            ItemIds.Blue   => new GridItem(ItemIds.Blue,   isObstacle: false, isMovable: true, health: 1),
            ItemIds.Yellow => new GridItem(ItemIds.Yellow, isObstacle: false, isMovable: true, health: 1),

            // Rockets: movable, not obstacle, 1 hp
            ItemIds.VerticalRocket   => new GridItem(ItemIds.VerticalRocket,   isObstacle: false, isMovable: true, health: 1),
            ItemIds.HorizontalRocket => new GridItem(ItemIds.HorizontalRocket, isObstacle: false, isMovable: true, health: 1),

            // Box: fixed in place, obstacle, 1 hp
            ItemIds.Box => new GridItem(ItemIds.Box, isObstacle: true, isMovable: false, health: 1),

            // Stone: fixed in place, obstacle, 1 hp (only damaged by rockets)
            ItemIds.Stone => new GridItem(ItemIds.Stone, isObstacle: true, isMovable: false, health: 1),

            // Vase: CAN fall (movable), obstacle, 2 hp
            ItemIds.Vase => new GridItem(ItemIds.Vase, isObstacle: true, isMovable: true, health: 2),

            // Empty / unknown
            ItemIds.None => CreateEmpty(),
            _ => CreateEmpty() // Fail gracefully for unknown IDs
        };
    }

    /// <summary>
    /// Convenience: create an empty cell.
    /// </summary>
    public static GridItem CreateEmpty()
    {
        return new GridItem(ItemIds.None, isObstacle: false, isMovable: false, health: 0);
    }

    /// <summary>
    /// Create a random cube (for gravity refill).
    /// Only produces color cubes — never rockets or obstacles.
    /// </summary>
    public static GridItem CreateRandomCube()
    {
        string id = ItemIds.CubeIds[_rand.Next(0, ItemIds.CubeIds.Length)];
        return CreateItem(id);
    }
}
