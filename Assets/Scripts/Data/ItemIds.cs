/// <summary>
/// Single source of truth for all item type identifiers.
/// These match the exact keys from the level file format defined in the Case Study:
/// r, g, b, y, rand, vro, hro, bo, s, v
/// </summary>
public static class ItemIds
{
    // --- Cubes (colors) ---
    public const string Red = "r";
    public const string Green = "g";
    public const string Blue = "b";
    public const string Yellow = "y";
    public const string Random = "rand";

    // --- Rockets ---
    public const string VerticalRocket = "vro";
    public const string HorizontalRocket = "hro";

    // --- Obstacles ---
    public const string Box = "bo";
    public const string Stone = "s";
    public const string Vase = "v";

    // --- Empty ---
    public const string None = "none";

    // --- Helper arrays for quick lookups ---
    public static readonly string[] CubeIds = { Red, Green, Blue, Yellow };
    public static readonly string[] RocketIds = { VerticalRocket, HorizontalRocket };
    public static readonly string[] ObstacleIds = { Box, Stone, Vase };

    public static bool IsCube(string id) =>
        id == Red || id == Green || id == Blue || id == Yellow;

    public static bool IsRocket(string id) =>
        id == VerticalRocket || id == HorizontalRocket;

    public static bool IsObstacle(string id) =>
        id == Box || id == Stone || id == Vase;
}
