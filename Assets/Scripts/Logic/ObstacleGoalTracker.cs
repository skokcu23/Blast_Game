using System.Collections.Generic;

/// <summary>
/// Tracks remaining obstacle counts per type for UI goal display and quick win-check.
/// Initialized from the Board, updated when obstacles are destroyed.
///
/// Usage:
///   var tracker = new ObstacleGoalTracker();
///   tracker.InitializeFromBoard(board);
///   // ... after a blast ...
///   tracker.OnObstacleDestroyed(ItemIds.Box);
///   bool won = tracker.AreAllGoalsMet();
///   int boxesLeft = tracker.GetRemainingCount(ItemIds.Box);
/// </summary>
public class ObstacleGoalTracker
{
    // Remaining count per obstacle type
    private Dictionary<string, int> _remaining = new Dictionary<string, int>();

    // Initial count per obstacle type (for UI: "2/4 boxes cleared")
    private Dictionary<string, int> _initial = new Dictionary<string, int>();

    /// <summary>
    /// Scan the board and count all living obstacles.
    /// Call this once when a level is loaded.
    /// </summary>
    public void InitializeFromBoard(Board board)
    {
        _remaining.Clear();
        _initial.Clear();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var item = board.GetItem(x, y);
                if (item.IsObstacle && item.IsAlive)
                {
                    if (!_remaining.ContainsKey(item.Id))
                    {
                        _remaining[item.Id] = 0;
                        _initial[item.Id] = 0;
                    }
                    _remaining[item.Id]++;
                    _initial[item.Id]++;
                }
            }
        }
    }

    /// <summary>
    /// Call when an obstacle is destroyed by a blast or rocket.
    /// </summary>
    public void OnObstacleDestroyed(string obstacleId)
    {
        if (_remaining.ContainsKey(obstacleId) && _remaining[obstacleId] > 0)
        {
            _remaining[obstacleId]--;
        }
    }

    /// <summary>
    /// Are all obstacles in the level cleared?
    /// </summary>
    public bool AreAllGoalsMet()
    {
        foreach (var kvp in _remaining)
        {
            if (kvp.Value > 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Get remaining count for a specific obstacle type.
    /// Returns 0 if the type was never present.
    /// </summary>
    public int GetRemainingCount(string obstacleId)
    {
        return _remaining.ContainsKey(obstacleId) ? _remaining[obstacleId] : 0;
    }

    /// <summary>
    /// Get the initial count for a specific obstacle type.
    /// Useful for UI: "2/4 cleared"
    /// </summary>
    public int GetInitialCount(string obstacleId)
    {
        return _initial.ContainsKey(obstacleId) ? _initial[obstacleId] : 0;
    }

    /// <summary>
    /// Get total remaining obstacles across all types.
    /// </summary>
    public int GetTotalRemaining()
    {
        int total = 0;
        foreach (var kvp in _remaining)
            total += kvp.Value;
        return total;
    }

    /// <summary>
    /// Get all obstacle types that are goals in this level.
    /// </summary>
    public List<string> GetGoalTypes()
    {
        return new List<string>(_initial.Keys);
    }
}
