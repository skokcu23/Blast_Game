using System.Collections.Generic;
using UnityEngine; // Only used for Random.Range

public class GravityProcessor
{
    // 1. GRAVITY LOGIC
    public List<GemMovement> ApplyGravity(Board board)
    {
        List<GemMovement> movements = new List<GemMovement>();

        // Scan column by column (bottom to top)
        for (int x = 0; x < board.Width; x++)
        {
            int emptySpacesCount = 0;

            for (int y = 0; y < board.Height; y++)
            {
                Coordinate current = new Coordinate(x, y);
                GridItem currentItem = board.GetItem(current);

                if (currentItem.IsEmpty) // Empty space found!
                {
                    emptySpacesCount++;
                }
                else if (emptySpacesCount > 0 && currentItem.IsMovable)
                {
                    // We found a gem, and there is empty space below it. Drop it!
                    Coordinate newCoord = new Coordinate(x, y - emptySpacesCount);

                    // Move the item down
                    board.SetItem(newCoord, currentItem);
                    board.SetItem(current, new GridItem(Gem.NONE, false, false, 0));

                    // Record the movement for the Unity visual layer
                    movements.Add(
                        new GemMovement
                        {
                            StartPos = current,
                            EndPos = newCoord,
                            GemType = currentItem.GemType,
                        }
                    );
                }
            }
        }
        return movements;
    }

    // 2. REFILL LOGIC
    public List<GemMovement> FillEmptySpaces(Board board)
    {
        List<GemMovement> newGems = new List<GemMovement>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate current = new Coordinate(x, y);

                if (board.GetItem(current).IsEmpty)
                {
                    // Generate a new random gem (Assuming your Gem enum goes from 1 to 4)
                    // If your Gem enum is different, update this to your GenerateRandomGem logic!
                    Gem randomGem = (Gem)Random.Range(0, 4);
                    GridItem newItem = new GridItem(randomGem);

                    // Update Board Memory
                    board.SetItem(current, newItem);

                    // Calculate a start position "above the board" so it slides onto the screen
                    Coordinate spawnPos = new Coordinate(x, board.Height + y);

                    newGems.Add(
                        new GemMovement
                        {
                            StartPos = spawnPos,
                            EndPos = current,
                            GemType = randomGem,
                        }
                    );
                }
            }
        }
        return newGems;
    }
}
