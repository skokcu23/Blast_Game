using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Thin wrapper: GameSession → BoardView + UI.
///
/// View Part 3 change: after every turn's animations complete,
/// calls BoardView.ReconcileWithBoard() BEFORE updating hints.
/// This guarantees _activeCubes is perfectly synced with the Board,
/// so hints always appear on the correct visuals.
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
            // 1. Animate all steps in order
            await AnimateTurnSteps(turnResult);

            // 2. RECONCILE — the key fix for all visual desync bugs
            _boardView.ReconcileWithBoard(_session.Board);

            // 3. Update hints (now guaranteed correct because reconciliation just ran)
            _boardView.UpdateRocketHints(turnResult.HintData);

            // 4. Update UI
            if (_gameplayUI != null)
            {
                _gameplayUI.UpdateMoves(turnResult.MovesRemaining);
                _gameplayUI.UpdateGoals(_session.GoalTracker);
            }

            OnMovesChanged?.Invoke(turnResult.MovesRemaining);
            OnGoalsUpdated?.Invoke(_session.GoalTracker);

            // 5. Handle win/lose
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
                    await Task.Delay(200);
                    break;

                case TurnStepType.RocketExplosion:
                    await _boardView.AnimateRocketExplosion(step.ExplosionData);
                    break;

                case TurnStepType.ComboExplosion:
                    List<Task> comboTasks = new List<Task>();
                    foreach (var data in step.ComboExplosionData)
                        comboTasks.Add(_boardView.AnimateRocketExplosion(data));
                    await Task.WhenAll(comboTasks);
                    break;

                case TurnStepType.Gravity:
                    await _boardView.AnimateGravity(step.GravityData);
                    break;

                case TurnStepType.Refill:
                    await _boardView.AnimateRefill(step.RefillData);
                    break;

                case TurnStepType.UpdateDamagedSprites:
                    _boardView.UpdateDamagedSprites(_session.Board, step.DamagedSpritesToUpdate);
                    break;
            }
        }
    }

    // ==========================================
    // GAME STATE → UI
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
