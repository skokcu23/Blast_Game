/// <summary>
/// Enumerates all possible sources of damage in the game.
/// Used by DamageResolver to apply per-source rules.
///
/// Case Study rules:
///   Blast  → damages Box, Vase (not Stone)
///   Rocket → damages Box, Stone, Vase
///   Combo  → damages Box, Stone, Vase (3x3 area, Iteration 3)
/// </summary>
public enum DamageSource
{
    Blast,
    Rocket,
    Combo
}
