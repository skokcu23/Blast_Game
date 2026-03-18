/// <summary>
/// States of a level play session.
/// </summary>
public enum GameSessionState
{
    /// <summary>Level is being played, accepting input</summary>
    Playing,

    /// <summary>All obstacles cleared within move limit</summary>
    Won,

    /// <summary>Ran out of moves with obstacles remaining</summary>
    Lost
}
