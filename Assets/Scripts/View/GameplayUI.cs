using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the in-game UI: goal display and move counter.
///
/// Layer separation: receives GoalSnapshot (lightweight data)
/// instead of ObstacleGoalTracker (logic layer object).
/// The Orchestrator builds snapshots as the layer mediator.
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("Move Counter")]
    [SerializeField] private TextMeshProUGUI _moveCountText;

    [Header("Goal Display")]
    [SerializeField] private Transform _goalContainer;
    [SerializeField] private GameObject _goalItemPrefab;

    [Header("Goal Sprites")]
    [SerializeField] private Sprite _boxGoalSprite;
    [SerializeField] private Sprite _stoneGoalSprite;
    [SerializeField] private Sprite _vaseGoalSprite;
    [SerializeField] private Sprite _goalCheckSprite;

    private Dictionary<string, GoalItemUI> _goalItems = new Dictionary<string, GoalItemUI>();

    /// <summary>
    /// Initialize goal display from a snapshot. Call once when level loads.
    /// </summary>
    public void InitializeGoals(GoalSnapshot snapshot)
    {
        foreach (Transform child in _goalContainer)
            Destroy(child.gameObject);
        _goalItems.Clear();

        foreach (var obstacleId in snapshot.GoalTypes)
        {
            if (!snapshot.InitialCounts.TryGetValue(obstacleId, out int count)) continue;
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
    /// Update goal counts from a snapshot.
    /// </summary>
    public void UpdateGoals(GoalSnapshot snapshot)
    {
        foreach (var kvp in _goalItems)
        {
            if (snapshot.RemainingCounts.TryGetValue(kvp.Key, out int remaining))
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
