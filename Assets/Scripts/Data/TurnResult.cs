using System.Collections.Generic;

/// <summary>
/// Complete result of processing one player tap.
/// Contains an ordered list of steps the View should animate sequentially.
///
/// The GameSession produces this; the View consumes it.
/// This is the single contract between Logic and View for a turn.
/// </summary>
public class TurnResult
{
    /// <summary>Was this a valid tap that consumed a move?</summary>
    public bool IsValid;

    /// <summary>Ordered list of events to animate</summary>
    public List<TurnStep> Steps;

    /// <summary>Game state after this turn (Playing, Won, Lost)</summary>
    public GameSessionState StateAfterTurn;

    /// <summary>Remaining moves after this turn</summary>
    public int MovesRemaining;

    /// <summary>Updated hint data after board settles</summary>
    public Dictionary<Coordinate, string> HintData;

    public TurnResult()
    {
        Steps = new List<TurnStep>();
        HintData = new Dictionary<Coordinate, string>();
    }

    public static TurnResult Invalid() => new TurnResult { IsValid = false };
}

/// <summary>
/// One discrete event within a turn. The View processes these in order.
/// Only one of the data fields is non-null depending on StepType.
/// </summary>
public class TurnStep
{
    public TurnStepType Type;

    // --- Data (only one is set per step, based on Type) ---
    public BlastResult BlastData;
    public RocketCreationData RocketCreationData;
    public RocketExplosionData ExplosionData;
    public List<RocketExplosionData> ComboExplosionData;
    public List<ItemMovement> GravityData;
    public List<ItemMovement> RefillData;
}

/// <summary>
/// Types of events within a turn.
///
/// Damage visuals (vase cracking) are NOT a step — they're derived state
/// handled internally by AnimationController.CrackDamagedVases using
/// each step's DamagedObstacles list.
/// </summary>
public enum TurnStepType
{
    /// <summary>Cubes popped (less than 4 group)</summary>
    Blast,

    /// <summary>Cubes merged toward tapped cell + obstacles damaged</summary>
    BlastForRocket,

    /// <summary>Rocket placed on board</summary>
    RocketCreated,

    /// <summary>Single rocket exploded (projectile paths)</summary>
    RocketExplosion,

    /// <summary>Combo: multiple explosions animated in parallel</summary>
    ComboExplosion,

    /// <summary>Items fell down</summary>
    Gravity,

    /// <summary>New items spawned from top</summary>
    Refill
}
