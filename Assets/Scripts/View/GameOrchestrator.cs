using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Layer mediator: Logic (GameSession) → View (BoardView).
///
/// The Orchestrator is the ONLY class that reads Board and passes
/// data to the View. It builds lightweight snapshots (obstacle health dict)
/// so the View never directly accesses logic layer objects.
///
/// Board access pattern:
///   Initialize:    Orchestrator passes Board to BoardView.Initialize (setup)
///   Per step:      Orchestrator builds health snapshot → passes dict to View
///   Reconcile:     Orchestrator passes Board to BoardView.ReconcileWithBoard (safety net)
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
    public event Action<ObstacleGoalTracker> OnGoalsUpdated;
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
            _gameplayUI.InitializeGoals(_session.GoalTracker);
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

            // Safety net — Orchestrator mediates Board access
            _boardView.ReconcileWithBoard(_session.Board);

            _boardView.UpdateRocketHints(turnResult.HintData);

            if (_gameplayUI != null)
            {
                _gameplayUI.UpdateMoves(turnResult.MovesRemaining);
                _gameplayUI.UpdateGoals(_session.GoalTracker);
            }

            OnMovesChanged?.Invoke(turnResult.MovesRemaining);
            OnGoalsUpdated?.Invoke(_session.GoalTracker);

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
    // HEALTH SNAPSHOT — Mediator builds View data from Logic state
    // ==========================================

    /// <summary>
    /// Build a lightweight snapshot of obstacle health from the Board.
    /// Only includes living vases — the only obstacle with visible health states.
    /// The View uses this to update crack visuals without accessing Board directly.
    /// </summary>
    private Dictionary<Coordinate, int> BuildObstacleHealthSnapshot()
    {
        Board board = _session.Board;
        var snapshot = new Dictionary<Coordinate, int>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var coord = new Coordinate(x, y);
                var item = board.GetItem(coord);
                if (item.Id == ItemIds.Vase && item.IsAlive)
                    snapshot[coord] = item.Health;
            }
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
                    await _boardView.AnimateBlast(step.BlastData, BuildObstacleHealthSnapshot());
                    break;

                case TurnStepType.BlastForRocket:
                    await _boardView.AnimateRocketCreation(step.BlastData, BuildObstacleHealthSnapshot());
                    break;

                case TurnStepType.RocketCreated:
                    _boardView.SpawnRocketVisual(step.RocketCreationData);
                    await Task.Delay(200);
                    break;

                case TurnStepType.RocketExplosion:
                    await _boardView.AnimateRocketExplosion(step.ExplosionData, BuildObstacleHealthSnapshot());
                    break;

                case TurnStepType.ComboExplosion:
                {
                    var healthSnapshot = BuildObstacleHealthSnapshot();
                    List<Task> comboTasks = new List<Task>();
                    foreach (var data in step.ComboExplosionData)
                        comboTasks.Add(_boardView.AnimateRocketExplosion(data, healthSnapshot));
                    await Task.WhenAll(comboTasks);
                    break;
                }

                case TurnStepType.Gravity:
                    await _boardView.AnimateGravity(step.GravityData);
                    break;

                case TurnStepType.Refill:
                    await _boardView.AnimateRefill(step.RefillData);
                    break;

                case TurnStepType.UpdateDamagedSprites:
                    // NO-OP: vase cracks are derived state.
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
