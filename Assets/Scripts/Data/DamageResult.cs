/// <summary>
/// Result of a single damage attempt on an obstacle.
/// Returned by DamageResolver.TryDamage() to inform the caller
/// whether damage was applied and whether the obstacle was destroyed.
/// </summary>
public struct DamageResult
{
    /// <summary>True if damage was applied. False if the obstacle is immune to this source.</summary>
    public bool WasApplied;

    /// <summary>True if the obstacle's health reached zero from this damage.</summary>
    public bool WasDestroyed;

    /// <summary>Health remaining after damage. 0 if destroyed, -1 if immune.</summary>
    public int RemainingHealth;

    /// <summary>Create a result indicating the obstacle is immune to this damage source.</summary>
    public static DamageResult Immune() => new DamageResult
    {
        WasApplied = false,
        WasDestroyed = false,
        RemainingHealth = -1
    };

    /// <summary>Create a result indicating damage was successfully applied.</summary>
    public static DamageResult Applied(int remainingHealth) => new DamageResult
    {
        WasApplied = true,
        WasDestroyed = remainingHealth <= 0,
        RemainingHealth = remainingHealth
    };
}
