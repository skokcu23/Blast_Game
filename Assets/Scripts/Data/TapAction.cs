/// <summary>
/// Describes the action triggered by a player tap on a cell.
/// Determined by TapResolver, consumed by GameSession to dispatch the correct handler.
/// </summary>
public enum TapAction
{
    /// <summary>Tap has no effect (empty cell, single cube, obstacle, out of bounds).</summary>
    None,

    /// <summary>Tap triggers a cube group blast (2+ adjacent same-color cubes).</summary>
    BlastGroup,

    /// <summary>Tap triggers a single rocket explosion (splits into 2 projectiles).</summary>
    ExplodeRocket,

    /// <summary>Tap triggers a rocket-rocket combo (3-wide explosion in both directions).</summary>
    RocketCombo
}
