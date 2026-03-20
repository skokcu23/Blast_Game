using System.Collections.Generic;

/// <summary>
/// Lightweight snapshot of goal state for the UI layer.
/// Built by the Orchestrator from ObstacleGoalTracker.
/// The View never accesses ObstacleGoalTracker directly.
///
/// Contains both initial counts (for setup) and remaining counts (for updates).
/// </summary>
public class GoalSnapshot
{
    /// <summary>
    /// Obstacle types that are goals, in display order.
    /// </summary>
    public List<string> GoalTypes;

    /// <summary>
    /// Initial count per obstacle type (for level start setup).
    /// </summary>
    public Dictionary<string, int> InitialCounts;

    /// <summary>
    /// Current remaining count per obstacle type.
    /// </summary>
    public Dictionary<string, int> RemainingCounts;

    public GoalSnapshot()
    {
        GoalTypes = new List<string>();
        InitialCounts = new Dictionary<string, int>();
        RemainingCounts = new Dictionary<string, int>();
    }
}
