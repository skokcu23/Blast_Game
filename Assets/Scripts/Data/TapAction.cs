/// <summary>
/// Describes what should happen when a player taps a cell.
/// Determined by TapResolver, consumed by GameOrchestrator.
/// </summary>
public enum TapAction
{
    /// <summary>Tap has no effect (empty cell, single cube, obstacle)</summary>
    None,

    /// <summary>Tap triggers a cube group blast (2+ adjacent same-color)</summary>
    BlastGroup,

    /// <summary>Tap triggers a single rocket explosion (splits into 2 moving parts)</summary>
    ExplodeRocket,

    /// <summary>Tap triggers a rocket combo (adjacent rockets merge into 3x3)</summary>
    RocketCombo
}
