using System.Collections.Generic;

/// <summary>
/// Complete result of processing one player tap.
///
/// Produced by GameSession.ProcessTap(). Consumed by GameOrchestrator
/// which iterates Steps sequentially, animating each one via BoardView.
///
/// This is the single contract between the Logic and View layers.
/// </summary>
public class TurnResult
{
    /// <summary>Whether this tap was valid and consumed a move.</summary>
    public bool IsValid;

    /// <summary>Ordered list of events the View should animate sequentially.</summary>
    public List<TurnStep> Steps;

    /// <summary>Game state after this turn completes (Playing, Won, or Lost).</summary>
    public GameSessionState StateAfterTurn;

    /// <summary>Moves remaining after this turn.</summary>
    public int MovesRemaining;

    /// <summary>Hint data for rocket-eligible groups after the board settles.</summary>
    public Dictionary<Coordinate, string> HintData;

    public TurnResult()
    {
        Steps = new List<TurnStep>();
        HintData = new Dictionary<Coordinate, string>();
    }

    /// <summary>Create an invalid result (tap had no effect, no move consumed).</summary>
    public static TurnResult Invalid() => new TurnResult { IsValid = false };
}

/// <summary>
/// One discrete event within a turn.
/// Only the data field matching the Type is populated; others are null.
/// </summary>
public class TurnStep
{
    /// <summary>What kind of event this step represents.</summary>
    public TurnStepType Type;

    /// <summary>Blast data (Type = Blast or BlastForRocket).</summary>
    public BlastResult BlastData;

    /// <summary>Rocket creation data (Type = RocketCreated).</summary>
    public RocketCreationData RocketCreationData;

    /// <summary>Single rocket explosion data (Type = RocketExplosion).</summary>
    public RocketExplosionData ExplosionData;

    /// <summary>Combo explosion data — multiple simultaneous explosions (Type = ComboExplosion).</summary>
    public List<RocketExplosionData> ComboExplosionData;

    /// <summary>Gravity movement data (Type = Gravity).</summary>
    public List<ItemMovement> GravityData;

    /// <summary>Refill movement data (Type = Refill).</summary>
    public List<ItemMovement> RefillData;
}

/// <summary>
/// Types of discrete events within a turn, animated in sequence by the View.
/// </summary>
public enum TurnStepType
{
    /// <summary>Cubes popped (group of 2–3).</summary>
    Blast,

    /// <summary>Cubes merged toward tapped cell (group of 4+, rocket will be created).</summary>
    BlastForRocket,

    /// <summary>Rocket placed on the board at the tapped cell.</summary>
    RocketCreated,

    /// <summary>Single rocket exploded with 2 projectiles.</summary>
    RocketExplosion,

    /// <summary>Rocket-rocket combo: multiple explosions animated in parallel.</summary>
    ComboExplosion,

    /// <summary>Items fell down to fill empty cells.</summary>
    Gravity,

    /// <summary>New items spawned from above the board.</summary>
    Refill
}
