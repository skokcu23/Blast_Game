using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Controller: bridges Logic and View layers.
///
/// Tap flow (3A):
///   1. TapResolver determines what the tap means
///   2. Dispatch to appropriate handler:
///      - BlastGroup → blast cubes, optionally create rocket, gravity, refill, hints
///      - ExplodeRocket → placeholder (Iteration 3B)
///      - RocketCombo → placeholder (Iteration 3B)
///   3. After board settles, recalculate rocket hints
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
    private RocketProcessor _rocketProcessor;
    private TapResolver _tapResolver;
    private ObstacleGoalTracker _goalTracker;
    private LevelData _currentLevel;

    // State
    private int _moveCount;
    private bool _isBusy;
    private bool _gameOver;

    // --- Events for UI (Iteration 4) ---
    public event Action<int> OnMovesChanged;
    public event Action<ObstacleGoalTracker> OnGoalsUpdated;
    public event Action OnLevelWon;
    public event Action OnLevelFailed;

    // --- Public accessors ---
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
        LoadLevel(1);
    }

    public void LoadLevel(int levelNumber)
    {
        _gameOver = false;

        // 1. Parse level data
        _currentLevel = LevelParser.LoadLevel(levelNumber);

        if (_currentLevel == null)
        {
            Debug.LogError($"[GameOrchestrator] Failed to load level {levelNumber}. Falling back to random.");
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
        _rocketProcessor = new RocketProcessor();
        _tapResolver = new TapResolver();

        // 3. Goal tracker
        _goalTracker = new ObstacleGoalTracker();
        _goalTracker.InitializeFromBoard(_board);

        // 4. Build visual board
        _boardView.Initialize(_board);

        _isBusy = false;

        // 5. Initial hint calculation
        RefreshHints();

        // 6. Notify UI
        OnMovesChanged?.Invoke(_moveCount);
        OnGoalsUpdated?.Invoke(_goalTracker);

        Debug.Log($"[GameOrchestrator] Level {levelNumber} loaded. " +
                  $"Grid: {_board.Width}x{_board.Height}, Moves: {_moveCount}, " +
                  $"Obstacles: {_goalTracker.GetTotalRemaining()}");
    }

    // ==========================================
    // TAP DISPATCH
    // ==========================================

    public async void OnCellTapped(Coordinate coord)
    {
        if (_isBusy || _gameOver || _moveCount <= 0)
            return;

        // 1. Resolve what this tap means
        TapResult tapResult = _tapResolver.Resolve(_board, coord, _matchStrategy);

        switch (tapResult.Action)
        {
            case TapAction.BlastGroup:
                await HandleBlastGroup(tapResult);
                break;

            case TapAction.ExplodeRocket:
                await HandleExplodeRocket(tapResult);
                break;

            case TapAction.RocketCombo:
                await HandleRocketCombo(tapResult);
                break;

            case TapAction.None:
            default:
                return; // No move spent
        }
    }

    // ==========================================
    // BLAST GROUP (cube match)
    // ==========================================

    private async Task HandleBlastGroup(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        List<Coordinate> matches = tapResult.MatchedCoordinates;
        Coordinate tapped = tapResult.TappedCoord;
        bool createsRocket = _rocketProcessor.QualifiesForRocket(matches.Count);

        // 1. Logic: blast cubes + adjacent obstacle damage
        BlastResult blastResult = _matchStrategy.Blast(_board, matches, tapped);
        UpdateGoalTracker(blastResult);

        // 2. View: animate differently based on rocket creation
        if (createsRocket)
        {
            // Cubes merge toward tapped cell, then rocket spawns
            await _boardView.AnimateRocketCreation(blastResult, tapped);

            // Place rocket on board (logic)
            RocketCreationData rocketData = _rocketProcessor.CreateRocket(_board, tapped, matches);

            // Show the rocket visually
            _boardView.SpawnRocketVisual(rocketData);

            Debug.Log($"[GameOrchestrator] Rocket created at {tapped}: {rocketData.RocketId}");
        }
        else
        {
            // Normal pop animation
            await _boardView.AnimateBlast(blastResult);
        }

        // 3. Update obstacle sprites (vase cracking)
        _boardView.UpdateDamagedSprites(_board, blastResult.DamagedObstacles);

        // 4. Gravity → Refill → Hints
        await SettleBoard();

        // 5. Win/Lose check
        CheckGameState();

        _isBusy = false;
    }

    // ==========================================
    // ROCKET EXPLOSION (Iteration 3B)
    // ==========================================

    private async Task HandleExplodeRocket(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        Debug.Log($"[GameOrchestrator] Rocket explosion at {tapResult.TappedCoord} — not yet implemented (3B)");

        // TODO (Iteration 3B):
        // 1. RocketProcessor.ExplodeRocket(board, coord) → RocketExplosionData
        // 2. Queue-based chain reaction loop
        // 3. Animate each explosion step
        // 4. SettleBoard() + CheckGameState()

        _isBusy = false;
    }

    // ==========================================
    // ROCKET COMBO (Iteration 3B)
    // ==========================================

    private async Task HandleRocketCombo(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        Debug.Log($"[GameOrchestrator] Rocket combo at {tapResult.TappedCoord} — not yet implemented (3B)");

        // TODO (Iteration 3B):
        // 1. RocketProcessor.ProcessCombo(board, tapped, adjacentRockets) → RocketExplosionData
        // 2. Queue-based chain reaction loop
        // 3. Animate combo explosion
        // 4. SettleBoard() + CheckGameState()

        _isBusy = false;
    }

    // ==========================================
    // SHARED: Board settling (gravity + refill + hints)
    // ==========================================

    private async Task SettleBoard()
    {
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        await _boardView.AnimateGravity(gravityMovements);

        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        await _boardView.AnimateRefill(refillMovements);

        RefreshHints();
    }

    // ==========================================
    // HINTS
    // ==========================================

    private void RefreshHints()
    {
        var hintData = HintCalculator.FindRocketHints(_board);
        _boardView.UpdateRocketHints(hintData);
    }
    // ==========================================
    // GOAL TRACKING + WIN/LOSE
    // ==========================================

    private void UpdateGoalTracker(BlastResult result)
    {
        foreach (var info in result.DestroyedObstacleInfos)
        {
            _goalTracker.OnObstacleDestroyed(info.ObstacleId);
        }
        OnGoalsUpdated?.Invoke(_goalTracker);
    }

    private void CheckGameState()
    {
        if (_board.AreAllObstaclesCleared())
        {
            _gameOver = true;
            Debug.Log("[GameOrchestrator] LEVEL WON!");
            OnLevelWon?.Invoke();
            return;
        }

        if (_moveCount <= 0)
        {
            _gameOver = true;
            Debug.Log("[GameOrchestrator] LEVEL FAILED — no moves remaining.");
            OnLevelFailed?.Invoke();
            return;
        }

        Debug.Log($"[GameOrchestrator] Moves: {_moveCount}, Obstacles: {_goalTracker.GetTotalRemaining()}");
    }
}
