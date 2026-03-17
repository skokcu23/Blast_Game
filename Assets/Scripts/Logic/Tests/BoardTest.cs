using NUnit.Framework;

public class BoardTests
{
    [Test]
    public void Board_Initialization_SetsCorrectDimensions()
    {
        // Arrange & Act
        Board board = new Board(8, 10);

        // Assert
        Assert.AreEqual(8, board.Width, "Board width should be 8.");
        Assert.AreEqual(10, board.Height, "Board height should be 10.");
    }

    [Test]
    public void Board_SetAndGetItem_WorksCorrectly()
    {
        // Arrange
        Board board = new Board(5, 5);

        // Initialize the single coordinate we are going to test
        board.SetItem(2, 2, new GridItem(Gem.NONE));

        GridItem newItem = new GridItem(Gem.BLUE);

        // Act
        board.SetItem(2, 2, newItem);
        Gem retrievedGem = board.GetItem(2, 2).GemType;

        // Assert
        Assert.AreEqual(
            Gem.BLUE,
            retrievedGem,
            "The board should return the exact gem that was set at the coordinate."
        );
    }

    [Test]
    public void Board_IsValidCoordinate_ReturnsTrueForInside_FalseForOutside()
    {
        // Arrange
        Board board = new Board(4, 4); // x: 0-3, y: 0-3

        // Act & Assert
        Assert.IsTrue(board.IsValidCoordinate(0, 0), "Bottom-left corner should be valid.");
        Assert.IsTrue(board.IsValidCoordinate(3, 3), "Top-right corner should be valid.");

        Assert.IsFalse(board.IsValidCoordinate(-1, 0), "Negative X should be invalid.");
        Assert.IsFalse(board.IsValidCoordinate(0, -1), "Negative Y should be invalid.");
        Assert.IsFalse(board.IsValidCoordinate(4, 0), "X equal to width should be invalid.");
        Assert.IsFalse(board.IsValidCoordinate(0, 4), "Y equal to height should be invalid.");
    }
}
