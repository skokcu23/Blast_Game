using System.Collections.Generic;

/// <summary>
/// Read-only snapshot of obstacle goal progress for the UI layer.
///
/// Built by GameOrchestrator from ObstacleGoalTracker after each turn.
/// The View layer never accesses ObstacleGoalTracker directly —
/// this DTO is the only bridge between goal state and UI rendering.
/// </summary>
public class GoalSnapshot
{
    /// <summary>Obstacle types that are goals for this level, in display order.</summary>
    public List<string> GoalTypes;

    /// <summary>How many of each obstacle type existed at level start.</summary>
    public Dictionary<string, int> InitialCounts;

    /// <summary>How many of each obstacle type remain on the board.</summary>
    public Dictionary<string, int> RemainingCounts;

    public GoalSnapshot()
    {
        GoalTypes = new List<string>();
        InitialCounts = new Dictionary<string, int>();
        RemainingCounts = new Dictionary<string, int>();
    }
}
