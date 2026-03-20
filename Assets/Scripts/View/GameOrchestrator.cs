using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Layer mediator: Logic (GameSession) → View (BoardView) + UI (GameplayUI).
///
/// Builds lightweight snapshots from logic state for the View and UI layers:
///   - GoalSnapshot for GameplayUI (instead of passing ObstacleGoalTracker)
///   - Board only accessed for Initialize and ReconcileWithBoard
///
/// No health snapshots. No GoalTracker passed to View.
/// No dead code. No magic numbers.
/// </summary>
public class GameOrchestrator : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private BoardView _boardView;
    [SerializeField] private InputManager _inputManager;

    [Header("UI References")]
    [SerializeField] private GameplayUI _gameplayUI;
    [SerializeField] private FailPopupUI _failPopup;
    [SerializeField] private CelebrationUI _celebrationUI;

    private GameSession _session;
    private LevelProgressionManager _progression;
    private int _currentLevelNumber;
    private bool _isBusy;

    public event Action<int> OnMovesChanged;
    public event Action OnLevelWon;
    public event Action OnLevelFailed;

    public GameSession Session => _session;
    public LevelProgressionManager Progression => _progression;

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
        var persistence = new PlayerPrefsLevelPersistence();
        int totalLevels = LevelParser.GetTotalLevelCount();
        _progression = new LevelProgressionManager(persistence, totalLevels);

        if (_failPopup != null)
            _failPopup.Initialize(this);

        int levelToPlay = _progression.GetLevelToPlay();
        if (levelToPlay == -1)
        {
            Debug.Log("[GameOrchestrator] All levels complete.");
            LoadLevel(1);
        }
        else
        {
            LoadLevel(levelToPlay);
        }
    }

    // ==========================================
    // LEVEL LOADING
    // ==========================================

    public void LoadLevel(int levelNumber)
    {
        _currentLevelNumber = levelNumber;

        LevelData levelData = LevelParser.LoadLevel(levelNumber);
        if (levelData == null)
        {
            Debug.LogError($"[GameOrchestrator] Failed to load level {levelNumber}.");
            return;
        }

        _session = new GameSession(levelData);
        _boardView.Initialize(_session.Board);
        _boardView.UpdateRocketHints(_session.GetHints());

        if (_gameplayUI != null)
        {
            _gameplayUI.UpdateMoves(_session.MovesRemaining);
            _gameplayUI.InitializeGoals(BuildGoalSnapshot());
        }

        _isBusy = false;

        Debug.Log($"[GameOrchestrator] Level {levelNumber} loaded. " +
                  $"Grid: {_session.Board.Width}x{_session.Board.Height}, " +
                  $"Moves: {_session.MovesRemaining}, " +
                  $"Obstacles: {_session.GoalTracker.GetTotalRemaining()}");
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(_currentLevelNumber);
    }

    // ==========================================
    // TAP HANDLING
    // ==========================================

    public async void OnCellTapped(Coordinate coord)
    {
        if (_isBusy) return;
        if (_session == null || _session.State != GameSessionState.Playing) return;

        TurnResult turnResult = _session.ProcessTap(coord);
        if (!turnResult.IsValid) return;

        _isBusy = true;

        try
        {
            await AnimateTurnSteps(turnResult);

            _boardView.ReconcileWithBoard(_session.Board);
            _boardView.UpdateRocketHints(turnResult.HintData);

            if (_gameplayUI != null)
            {
                _gameplayUI.UpdateMoves(turnResult.MovesRemaining);
                _gameplayUI.UpdateGoals(BuildGoalSnapshot());
            }

            OnMovesChanged?.Invoke(turnResult.MovesRemaining);

            HandleGameState(turnResult.StateAfterTurn);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameOrchestrator] Exception: {ex}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    // ==========================================
    // SNAPSHOT BUILDERS — Mediator extracts data for View/UI
    // ==========================================

    /// <summary>
    /// Build a lightweight goal snapshot from ObstacleGoalTracker.
    /// GameplayUI receives this instead of the tracker itself.
    /// </summary>
    private GoalSnapshot BuildGoalSnapshot()
    {
        var tracker = _session.GoalTracker;
        var snapshot = new GoalSnapshot();

        snapshot.GoalTypes = tracker.GetGoalTypes();

        foreach (var obstacleId in snapshot.GoalTypes)
        {
            snapshot.InitialCounts[obstacleId] = tracker.GetInitialCount(obstacleId);
            snapshot.RemainingCounts[obstacleId] = tracker.GetRemainingCount(obstacleId);
        }

        return snapshot;
    }

    // ==========================================
    // STEP DISPATCHER
    // ==========================================

    private async Task AnimateTurnSteps(TurnResult turnResult)
    {
        foreach (var step in turnResult.Steps)
        {
            switch (step.Type)
            {
                case TurnStepType.Blast:
                    await _boardView.AnimateBlast(step.BlastData);
                    break;

                case TurnStepType.BlastForRocket:
                    await _boardView.AnimateRocketCreation(step.BlastData);
                    break;

                case TurnStepType.RocketCreated:
                    _boardView.SpawnRocketVisual(step.RocketCreationData);
                    // Fix 2: DOTween delay respects Time.timeScale (Task.Delay doesn't)
                    await DOVirtual.DelayedCall(
                        _boardView.RocketSpawnPause, () => { }, false
                    ).ToTask();
                    break;

                case TurnStepType.RocketExplosion:
                    await _boardView.AnimateRocketExplosion(step.ExplosionData);
                    break;

                case TurnStepType.ComboExplosion:
                    // Fix 1: single method handles cleanup once + parallel projectiles
                    await _boardView.AnimateComboExplosion(step.ComboExplosionData);
                    break;

                case TurnStepType.Gravity:
                    await _boardView.AnimateGravity(step.GravityData);
                    break;

                case TurnStepType.Refill:
                    await _boardView.AnimateRefill(step.RefillData);
                    break;
            }
        }
    }

    // ==========================================
    // GAME STATE
    // ==========================================

    private void HandleGameState(GameSessionState state)
    {
        switch (state)
        {
            case GameSessionState.Won:
                Debug.Log("[GameOrchestrator] LEVEL WON!");
                _progression.AdvanceLevel();
                OnLevelWon?.Invoke();
                if (_celebrationUI != null)
                    _celebrationUI.PlayCelebration();
                break;

            case GameSessionState.Lost:
                Debug.Log("[GameOrchestrator] LEVEL FAILED!");
                OnLevelFailed?.Invoke();
                if (_failPopup != null)
                    _failPopup.Show();
                break;
        }
    }
}
