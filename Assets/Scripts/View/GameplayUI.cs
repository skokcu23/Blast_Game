using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the in-game UI: goal display and move counter.
///
/// Case Study images show:
///   - Top left: "Goal" label with obstacle icons and remaining counts
///   - Top right: "Move" label with a number
///   - Goal icons get a checkmark overlay when cleared
///
/// The Orchestrator calls UpdateMoves() and UpdateGoals() via events.
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("Move Counter")]
    [SerializeField] private TextMeshProUGUI _moveCountText;

    [Header("Goal Display")]
    [SerializeField] private Transform _goalContainer; // Parent for goal icons
    [SerializeField] private GameObject _goalItemPrefab; // Prefab with icon + count text

    [Header("Goal Sprites")]
    [SerializeField] private Sprite _boxGoalSprite;
    [SerializeField] private Sprite _stoneGoalSprite;
    [SerializeField] private Sprite _vaseGoalSprite;
    [SerializeField] private Sprite _goalCheckSprite; // Checkmark overlay

    private Dictionary<string, GoalItemUI> _goalItems = new Dictionary<string, GoalItemUI>();

    /// <summary>
    /// Initialize goals from the tracker. Call once when level loads.
    /// </summary>
    public void InitializeGoals(ObstacleGoalTracker tracker)
    {
        // Clear existing goal items
        foreach (Transform child in _goalContainer)
            Destroy(child.gameObject);
        _goalItems.Clear();

        var goalTypes = tracker.GetGoalTypes();

        foreach (var obstacleId in goalTypes)
        {
            int count = tracker.GetInitialCount(obstacleId);
            if (count <= 0) continue;

            GameObject go = Instantiate(_goalItemPrefab, _goalContainer);
            GoalItemUI goalItem = go.GetComponent<GoalItemUI>();

            if (goalItem != null)
            {
                Sprite icon = GetGoalSprite(obstacleId);
                goalItem.Setup(icon, count, _goalCheckSprite);
                _goalItems[obstacleId] = goalItem;
            }
        }
    }

    /// <summary>
    /// Update move counter display.
    /// </summary>
    public void UpdateMoves(int remaining)
    {
        if (_moveCountText != null)
            _moveCountText.text = remaining.ToString();
    }

    /// <summary>
    /// Update goal counts from tracker.
    /// </summary>
    public void UpdateGoals(ObstacleGoalTracker tracker)
    {
        foreach (var kvp in _goalItems)
        {
            int remaining = tracker.GetRemainingCount(kvp.Key);
            kvp.Value.UpdateCount(remaining);
        }
    }

    private Sprite GetGoalSprite(string obstacleId)
    {
        return obstacleId switch
        {
            ItemIds.Box => _boxGoalSprite,
            ItemIds.Stone => _stoneGoalSprite,
            ItemIds.Vase => _vaseGoalSprite,
            _ => null
        };
    }
}
