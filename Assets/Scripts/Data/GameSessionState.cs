/// <summary>
/// Lifecycle states of a single level play session.
/// Managed by GameSession, read by GameOrchestrator to trigger UI.
/// </summary>
public enum GameSessionState
{
    /// <summary>Level is in progress, accepting player input.</summary>
    Playing,

    /// <summary>All obstacles cleared within the move limit. Level complete.</summary>
    Won,

    /// <summary>No moves remaining with obstacles still on the board.</summary>
    Lost
}
