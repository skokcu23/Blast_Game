/// <summary>
/// Source of damage applied to an obstacle.
/// DamageResolver uses this to determine immunity rules per obstacle type.
///
/// Damage rules:
///   Blast  → damages Box and Vase (Stone is immune)
///   Rocket → damages Box, Stone, and Vase
///   Combo  → damages Box, Stone, and Vase (treated as Rocket-level per direction)
/// </summary>
public enum DamageSource
{
    /// <summary>Damage from a cube group blast (adjacent to the group).</summary>
    Blast,

    /// <summary>Damage from a single rocket projectile passing over.</summary>
    Rocket,

    /// <summary>Damage from a rocket-rocket combo explosion.</summary>
    Combo
}
