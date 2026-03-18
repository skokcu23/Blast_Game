/// <summary>
/// DTO returned by DamageResolver after attempting to damage an item.
/// Tells the caller exactly what happened so they can update BlastResult.
/// </summary>
public struct DamageResult
{
    /// <summary>Was damage actually applied? (False if immune, e.g. Stone vs Blast)</summary>
    public bool WasApplied;

    /// <summary>Was the item destroyed by this damage?</summary>
    public bool WasDestroyed;

    /// <summary>Remaining health after damage (0 if destroyed)</summary>
    public int RemainingHealth;

    public static DamageResult Immune() => new DamageResult
    {
        WasApplied = false,
        WasDestroyed = false,
        RemainingHealth = -1
    };

    public static DamageResult Applied(int remainingHealth) => new DamageResult
    {
        WasApplied = true,
        WasDestroyed = remainingHealth <= 0,
        RemainingHealth = remainingHealth
    };
}
