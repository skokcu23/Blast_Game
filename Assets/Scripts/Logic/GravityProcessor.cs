using System.Collections.Generic;

/// <summary>
/// Pure C# gravity and refill logic. No UnityEngine dependency.
/// 
/// Key rules from Case Study:
/// - Items fall vertically into empty cells
/// - Non-movable items (Box, Stone) block falling — cubes cannot pass through them
/// - Movable obstacles (Vase) DO fall
/// - New random cubes spawn from above to fill remaining gaps
/// </summary>
public class GravityProcessor
{
    /// <summary>
    /// Drop all movable items down into empty spaces below them.
    /// Scans each column bottom-to-top, counting empty slots.
    /// Non-movable items (Box, Stone) reset the empty counter since nothing falls through them.
    /// </summary>
    public List<ItemMovement> ApplyGravity(Board board)
    {
        List<ItemMovement> movements = new List<ItemMovement>();

        for (int x = 0; x < board.Width; x++)
        {
            int emptyCount = 0;

            for (int y = 0; y < board.Height; y++)
            {
                Coordinate current = new Coordinate(x, y);
                GridItem item = board.GetItem(current);

                if (item.IsEmpty)
                {
                    emptyCount++;
                }
                else if (!item.IsMovable)
                {
                    // Non-movable item (Box, Stone) acts as a floor.
                    // Nothing above can fall past it, so reset counter.
                    emptyCount = 0;
                }
                else if (emptyCount > 0)
                {
                    // Movable item with empty space below — drop it
                    Coordinate target = new Coordinate(x, y - emptyCount);

                    board.SetItem(target, item);
                    board.SetItem(current, ItemFactory.CreateEmpty());

                    movements.Add(new ItemMovement
                    {
                        StartPos = current,
                        EndPos = target,
                        ItemId = item.Id
                    });
                }
            }
        }

        return movements;
    }

    /// <summary>
    /// Fill remaining empty cells with new random cubes falling from above.
    /// StartPos is above the visible board so the View can animate them sliding in.
    /// </summary>
    public List<ItemMovement> FillEmptySpaces(Board board)
    {
        List<ItemMovement> newItems = new List<ItemMovement>();

        for (int x = 0; x < board.Width; x++)
        {
            // Count how many new items we've spawned in this column
            // (used to stagger their spawn positions above the board)
            int spawnOffset = 0;

            for (int y = 0; y < board.Height; y++)
            {
                Coordinate current = new Coordinate(x, y);

                if (board.GetItem(current).IsEmpty)
                {
                    GridItem newItem = ItemFactory.CreateRandomCube();
                    board.SetItem(current, newItem);

                    // Spawn position is above the board, staggered so
                    // multiple new items in the same column don't overlap
                    Coordinate spawnPos = new Coordinate(x, board.Height + spawnOffset);
                    spawnOffset++;

                    newItems.Add(new ItemMovement
                    {
                        StartPos = spawnPos,
                        EndPos = current,
                        ItemId = newItem.Id
                    });
                }
            }
        }

        return newItems;
    }
}
