using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Controller: bridges Logic and View layers.
/// Listens for input → asks Logic to calculate → awaits View animations → checks win/lose.
/// Emits events for UI systems to react to (move count changes, win, lose).
/// </summary>
public class GameOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardView _boardView;
    [SerializeField] private InputManager _inputManager;

    // Logic layer (pure C#)
    private Board _board;
    private MatchStrategy _matchStrategy;
    private GravityProcessor _gravityProcessor;
    private ObstacleGoalTracker _goalTracker;
    private LevelData _currentLevel;

    // State
    private int _moveCount;
    private bool _isBusy;
    private bool _gameOver;

    // --- Events for UI (Iteration 4 will connect these) ---
    public event Action<int> OnMovesChanged;           // remaining moves
    public event Action<ObstacleGoalTracker> OnGoalsUpdated; // after any obstacle change
    public event Action OnLevelWon;
    public event Action OnLevelFailed;

    // --- Public accessors for UI ---
    public int MovesRemaining => _moveCount;
    public ObstacleGoalTracker GoalTracker => _goalTracker;

    // --- Event Subscription ---
    private void OnEnable()
    {
        if (_inputManager != null)
            _inputManager.OnCubeTapped += OnCellTapped;
    }

    private void OnDisable()
    {
        if (_inputManager != null)
            _inputManager.OnCubeTapped -= OnCellTapped;
    }

    void Start()
    {
        // TODO: Get level number from a GameManager/SceneLoader (Iteration 4)
        LoadLevel(1);
    }

    /// <summary>
    /// Initialize everything for a given level number.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        _gameOver = false;

        // 1. Parse level data
        _currentLevel = LevelParser.LoadLevel(levelNumber);

        if (_currentLevel == null)
        {
            Debug.LogError($"[GameOrchestrator] Failed to load level {levelNumber}. " +
                           "Falling back to random 8x10 board.");
            _board = new Board(8, 10);
            _board.InitializeRandom();
            _moveCount = 25;
        }
        else
        {
            _board = new Board(_currentLevel.grid_width, _currentLevel.grid_height);
            _board.Initialize(_currentLevel);
            _moveCount = _currentLevel.move_count;
        }

        // 2. Initialize systems
        _matchStrategy = new ClassicMatchStrategy();
        _gravityProcessor = new GravityProcessor();

        // 3. Initialize goal tracker
        _goalTracker = new ObstacleGoalTracker();
        _goalTracker.InitializeFromBoard(_board);

        // 4. Build the visual board
        _boardView.Initialize(_board);

        _isBusy = false;

        // 5. Notify UI
        OnMovesChanged?.Invoke(_moveCount);
        OnGoalsUpdated?.Invoke(_goalTracker);

        Debug.Log($"[GameOrchestrator] Level {levelNumber} loaded. " +
                  $"Grid: {_board.Width}x{_board.Height}, " +
                  $"Moves: {_moveCount}, " +
                  $"Obstacles: {_goalTracker.GetTotalRemaining()}");
    }

    public async void OnCellTapped(Coordinate coord)
    {
        if (_isBusy || _gameOver || _moveCount <= 0)
            return;

        // 1. Find matches
        var matches = _matchStrategy.FindMatches(_board, coord);

        if (matches.Count < 2)
            return;

        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        // 2. Execute blast (clears cubes, DamageResolver handles obstacles)
        BlastResult blastResult = _matchStrategy.Blast(_board, matches, coord);

        // 3. Update goal tracker for any destroyed obstacles
        foreach (var destroyed in blastResult.DestroyedObstacles)
        {
            // The item is already cleared from board, but we saved the coordinate.
            // We need the original item ID — store it in BlastResult for proper tracking.
            // For now, we rely on the tracker being notified.
            // NOTE: See DestroyedObstacleInfo enhancement below.
        }
        UpdateGoalTracker(blastResult);

        // 4. Animate blast + obstacle damage/destruction + sprite swaps
        await _boardView.AnimateBlast(blastResult);
        _boardView.UpdateDamagedSprites(_board, blastResult.DamagedObstacles);

        // 5. Rocket creation placeholder (Iteration 3)
        if (blastResult.ShouldCreateRocket)
        {
            Debug.Log($"[GameOrchestrator] Rocket should spawn at {coord} (Iteration 3)");
        }

        // 6. Gravity
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        await _boardView.AnimateGravity(gravityMovements);

        // 7. Refill
        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        await _boardView.AnimateRefill(refillMovements);

        // 8. Check win/lose
        CheckGameState();

        _isBusy = false;
    }

    private void UpdateGoalTracker(BlastResult result)
    {
        // For destroyed obstacles, we need to know what type they were.
        // Since the board cell is already cleared, we use the info list.
        foreach (var info in result.DestroyedObstacleInfos)
        {
            _goalTracker.OnObstacleDestroyed(info.ObstacleId);
        }

        OnGoalsUpdated?.Invoke(_goalTracker);
    }

    private void CheckGameState()
    {
        // Win: all obstacles cleared (use board as authoritative source)
        if (_board.AreAllObstaclesCleared())
        {
            _gameOver = true;
            Debug.Log("[GameOrchestrator] LEVEL WON!");
            OnLevelWon?.Invoke();
            return;
        }

        // Lose: no moves left but obstacles remain
        if (_moveCount <= 0)
        {
            _gameOver = true;
            Debug.Log("[GameOrchestrator] LEVEL FAILED — no moves remaining.");
            OnLevelFailed?.Invoke();
            return;
        }

        Debug.Log($"[GameOrchestrator] Moves: {_moveCount}, " +
                  $"Obstacles remaining: {_goalTracker.GetTotalRemaining()}");
    }
}
