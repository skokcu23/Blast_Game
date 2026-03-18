using System.Collections.Generic;

/// <summary>
/// DTO: Everything the View needs to animate a rocket being created.
///
/// Animation sequence:
///   1. Cubes in SourceCoordinates slide toward SpawnPosition
///   2. All cubes disappear at SpawnPosition
///   3. Rocket appears at SpawnPosition with RocketId sprite
/// </summary>
public class RocketCreationData
{
    /// <summary>Where the rocket spawns (the tapped cell)</summary>
    public Coordinate SpawnPosition;

    /// <summary>"vro" or "hro" — determines sprite and future explosion direction</summary>
    public string RocketId;

    /// <summary>All matched cube positions that merge into the rocket</summary>
    public List<Coordinate> SourceCoordinates;
}
