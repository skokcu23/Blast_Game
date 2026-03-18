using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class GameSessionTests
{
    // Helper: build a board with specific items
    private Board BuildBoard(int w, int h, Dictionary<Coordinate, string> items)
    {
        var board = new Board(w, h);
        foreach (var kvp in items)
            board.SetItem(kvp.Key, ItemFactory.CreateItem(kvp.Value));
        return board;
    }

    // ==========================================
    // BASIC TAP → BLAST
    // ==========================================

    [Test]
    public void ProcessTap_ValidBlast_ReturnsValidResult()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(9, result.MovesRemaining);
    }

    [Test]
    public void ProcessTap_InvalidTap_ReturnsInvalid()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        // Only 1 cube — no valid match

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(10, session.MovesRemaining); // No move spent
    }

    [Test]
    public void ProcessTap_EmptyCell_ReturnsInvalid()
    {
        var board = new Board(8, 8);
        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.IsFalse(result.IsValid);
    }

    [Test]
    public void ProcessTap_Blast_ContainsBlastStep()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(1, 0));

        var blastStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.Blast);
        Assert.IsNotNull(blastStep);
        Assert.AreEqual(3, blastStep.BlastData.BlastedCoordinates.Count);
    }

    [Test]
    public void ProcessTap_Blast_ClearsBoardCells()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        // After full turn (gravity+refill), cells are filled again.
        // Verify via the BlastResult inside the step instead.
        var blastStep = result.Steps.First(s => s.Type == TurnStepType.Blast);
        CollectionAssert.Contains(blastStep.BlastData.BlastedCoordinates, new Coordinate(0, 0));
        CollectionAssert.Contains(blastStep.BlastData.BlastedCoordinates, new Coordinate(1, 0));
    }

    // ==========================================
    // BLAST WITH GRAVITY + REFILL
    // ==========================================

    [Test]
    public void ProcessTap_Blast_IncludesGravityAndRefill()
    {
        var board = new Board(8, 8);
        // Two reds at bottom, blue above
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        // Should have gravity step (blue falls)
        bool hasGravity = result.Steps.Any(s => s.Type == TurnStepType.Gravity);
        Assert.IsTrue(hasGravity);

        // Should have refill step
        bool hasRefill = result.Steps.Any(s => s.Type == TurnStepType.Refill);
        Assert.IsTrue(hasRefill);
    }

    // ==========================================
    // ROCKET CREATION
    // ==========================================

    [Test]
    public void ProcessTap_GroupOf4_CreatesRocket()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(1, 0));

        var rocketStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.RocketCreated);
        Assert.IsNotNull(rocketStep, "Should contain a RocketCreated step");

        var blastStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.BlastForRocket);
        Assert.IsNotNull(blastStep, "Should use BlastForRocket (not Blast) for group >= 4");
    }

    [Test]
    public void ProcessTap_GroupOf4_RocketOnBoard()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        session.ProcessTap(new Coordinate(1, 0));

        // After gravity + refill, the rocket should exist somewhere on the board
        bool rocketExists = false;
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (board.GetItem(x, y).IsRocket)
                    rocketExists = true;

        Assert.IsTrue(rocketExists, "A rocket should exist on the board after group >= 4 blast");
    }

    [Test]
    public void ProcessTap_GroupOf3_NoRocketCreated()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(1, 0));

        var rocketStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.RocketCreated);
        Assert.IsNull(rocketStep, "Group of 3 should NOT create a rocket");
    }

    // ==========================================
    // ROCKET EXPLOSION
    // ==========================================


    [Test]
    public void ProcessTap_TapRocket_ExplodesIt()
    {
        var board = new Board(8, 8);
        board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(3, 3));

        Assert.IsTrue(result.IsValid);
        var explosionStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.RocketExplosion);
        Assert.IsNotNull(explosionStep, "Tapping rocket should produce an explosion step");

        // Verify red cube was in the explosion's destroyed list (cell refilled after gravity)
        CollectionAssert.Contains(explosionStep.ExplosionData.DestroyedCubes, new Coordinate(5, 3));
    }

    [Test]
    public void ProcessTap_RocketChain_BothExplode()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 3));

        // Verify via explosion steps (cells refilled after gravity)
        var explosionSteps = result.Steps.Where(s => s.Type == TurnStepType.RocketExplosion).ToList();
        Assert.AreEqual(2, explosionSteps.Count, "Chain: A should trigger B, producing 2 explosion steps");
    }

    [Test]
    public void ProcessTap_RocketChain_ABC()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        board.SetItem(5, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));
        board.SetItem(5, 7, ItemFactory.CreateItem(ItemIds.HorizontalRocket));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 3));

        var explosionSteps = result.Steps.Where(s => s.Type == TurnStepType.RocketExplosion).ToList();
        Assert.AreEqual(3, explosionSteps.Count, "A→B→C chain: 3 explosion steps");
    }

    // ==========================================
    // ROCKET COMBO
    // ==========================================

    [Test]
    public void ProcessTap_AdjacentRockets_ComboFires()
    {
        var board = new Board(8, 8);
        board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        board.SetItem(4, 3, ItemFactory.CreateItem(ItemIds.VerticalRocket));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(3, 3));

        var comboStep = result.Steps.FirstOrDefault(s => s.Type == TurnStepType.ComboExplosion);
        Assert.IsNotNull(comboStep, "Adjacent rockets should produce a ComboExplosion step");
        Assert.AreEqual(2, comboStep.ComboExplosionData.Count, "Combo should have 2 explosion data (H + V)");
    }

    // ==========================================
    // OBSTACLE DAMAGE
    // ==========================================

    [Test]
    public void ProcessTap_BlastAdjacentToBox_DestroysBox()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        // Box was destroyed — verify via GoalTracker (refill may have filled the cell)
        Assert.AreEqual(0, session.GoalTracker.GetRemainingCount(ItemIds.Box),
            "Box adjacent to blast should be destroyed");
    }

    [Test]
    public void ProcessTap_BlastAdjacentToStone_StoneImmune()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Stone));

        var session = new GameSession(board, 10);
        session.ProcessTap(new Coordinate(0, 0));

        Assert.AreEqual(ItemIds.Stone, board.GetItem(2, 0).Id);
        Assert.AreEqual(1, board.GetItem(2, 0).Health);
    }

    [Test]
    public void ProcessTap_RocketHitsStone_DestroysStone()
    {
        var board = new Board(8, 8);
        board.SetItem(3, 3, ItemFactory.CreateItem(ItemIds.HorizontalRocket));
        board.SetItem(6, 3, ItemFactory.CreateItem(ItemIds.Stone));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(3, 3));

        // Verify via GoalTracker (cell refilled after gravity)
        Assert.AreEqual(0, session.GoalTracker.GetRemainingCount(ItemIds.Stone),
            "Rocket should destroy stone");
    }

    [Test]
    public void ProcessTap_BlastAdjacentToVase_OneDamage()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Vase));

        var session = new GameSession(board, 10);
        session.ProcessTap(new Coordinate(0, 0));

        Assert.AreEqual(1, board.GetItem(2, 0).Health, "Vase should have 1 HP after blast");
    }

    // ==========================================
    // WIN / LOSE DETECTION
    // ==========================================

    [Test]
    public void ProcessTap_ClearAllObstacles_StateWon()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box)); // Only obstacle

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.AreEqual(GameSessionState.Won, result.StateAfterTurn);
        Assert.AreEqual(GameSessionState.Won, session.State);
    }

    [Test]
    public void ProcessTap_LastMoveObstaclesRemain_StateLost()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(5, 5, ItemFactory.CreateItem(ItemIds.Box)); // Far away, won't be damaged

        var session = new GameSession(board, 1); // Only 1 move
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.AreEqual(GameSessionState.Lost, result.StateAfterTurn);
    }

    [Test]
    public void ProcessTap_AfterGameWon_ReturnsInvalid()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));

        var session = new GameSession(board, 10);
        session.ProcessTap(new Coordinate(0, 0)); // Wins

        // Second tap should be rejected
        var result2 = session.ProcessTap(new Coordinate(3, 3));
        Assert.IsFalse(result2.IsValid);
    }

    [Test]
    public void ProcessTap_AfterGameLost_ReturnsInvalid()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(5, 5, ItemFactory.CreateItem(ItemIds.Box));

        var session = new GameSession(board, 1);
        session.ProcessTap(new Coordinate(0, 0)); // Loses

        var result2 = session.ProcessTap(new Coordinate(0, 0));
        Assert.IsFalse(result2.IsValid);
    }

    [Test]
    public void ProcessTap_NoObstaclesOnBoard_WinsImmediately()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        // No obstacles at all

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        Assert.AreEqual(GameSessionState.Won, result.StateAfterTurn);
    }

    // ==========================================
    // MOVE COUNTING
    // ==========================================

    [Test]
    public void ProcessTap_MultipleMoves_DecrementsCorrectly()
    {
        var board = new Board(8, 8);
        board.InitializeRandom();

        var session = new GameSession(board, 20);

        // Find a valid match and tap it
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var result = session.ProcessTap(new Coordinate(x, y));
                if (result.IsValid)
                {
                    Assert.AreEqual(19, session.MovesRemaining);
                    return;
                }
            }
        }

        // If we get here, no valid match was found (extremely unlikely with random board)
        Assert.Inconclusive("No valid match found on random board");
    }

    [Test]
    public void ProcessTap_InvalidTap_DoesNotDecrementMoves()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        session.ProcessTap(new Coordinate(0, 0)); // Single cube, invalid

        Assert.AreEqual(10, session.MovesRemaining);
    }

    // ==========================================
    // GOAL TRACKER INTEGRATION
    // ==========================================

    [Test]
    public void GoalTracker_TracksObstaclesCorrectly()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Box));
        board.SetItem(5, 5, ItemFactory.CreateItem(ItemIds.Box));

        var session = new GameSession(board, 10);

        Assert.AreEqual(2, session.GoalTracker.GetRemainingCount(ItemIds.Box));

        session.ProcessTap(new Coordinate(0, 0)); // Destroys one box

        Assert.AreEqual(1, session.GoalTracker.GetRemainingCount(ItemIds.Box));
    }

    // ==========================================
    // HINTS
    // ==========================================

    [Test]
    public void GetHints_ReturnsCorrectData()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Green));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Green));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Green));
        board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.Green));

        var session = new GameSession(board, 10);
        var hints = session.GetHints();

        Assert.AreEqual(4, hints.Count);
        Assert.IsTrue(hints.ContainsKey(new Coordinate(0, 0)));
        Assert.AreEqual(ItemIds.Green, hints[new Coordinate(0, 0)]);
    }

    [Test]
    public void ProcessTap_HintsUpdatedAfterTurn()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        // HintData should be populated (may be empty if no groups >= 4 after refill)
        Assert.IsNotNull(result.HintData);
    }

    // ==========================================
    // STEP ORDER VALIDATION
    // ==========================================

    [Test]
    public void ProcessTap_Blast_StepOrderIsCorrect()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Red));
        board.SetItem(0, 1, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(0, 0));

        // Order should be: Blast → Gravity → Refill
        var types = result.Steps.Select(s => s.Type).ToList();
        int blastIdx = types.IndexOf(TurnStepType.Blast);
        int gravityIdx = types.IndexOf(TurnStepType.Gravity);
        int refillIdx = types.IndexOf(TurnStepType.Refill);

        Assert.Greater(gravityIdx, blastIdx, "Gravity should come after Blast");
        Assert.Greater(refillIdx, gravityIdx, "Refill should come after Gravity");
    }

    [Test]
    public void ProcessTap_RocketCreation_StepOrderIsCorrect()
    {
        var board = new Board(8, 8);
        board.SetItem(0, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(1, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(2, 0, ItemFactory.CreateItem(ItemIds.Blue));
        board.SetItem(3, 0, ItemFactory.CreateItem(ItemIds.Blue));

        var session = new GameSession(board, 10);
        var result = session.ProcessTap(new Coordinate(1, 0));

        var types = result.Steps.Select(s => s.Type).ToList();
        int blastIdx = types.IndexOf(TurnStepType.BlastForRocket);
        int rocketIdx = types.IndexOf(TurnStepType.RocketCreated);

        Assert.Greater(rocketIdx, blastIdx, "RocketCreated should come after BlastForRocket");
    }
}
