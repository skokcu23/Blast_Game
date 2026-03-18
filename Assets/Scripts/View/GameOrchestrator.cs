using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Thin wrapper: GameSession (logic) → BoardView (animations) + UI.
///
/// Scene flow:
///   MainScene → LevelButton tap → LevelScene loads → GameOrchestrator.Start()
///   Win → CelebrationUI → MainScene
///   Lose → FailPopupUI → Close (MainScene) or TryAgain (reload)
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

    // --- Pure C# systems ---
    private GameSession _session;
    private LevelProgressionManager _progression;
    private int _currentLevelNumber;

    // --- State ---
    private bool _isBusy;

    // --- Events (for any additional listeners) ---
    public event Action<int> OnMovesChanged;
    public event Action<ObstacleGoalTracker> OnGoalsUpdated;
    public event Action OnLevelWon;
    public event Action OnLevelFailed;

    // --- Public accessors ---
    public GameSession Session => _session;
    public LevelProgressionManager Progression => _progression;

    // ==========================================
    // LIFECYCLE
    // ==========================================

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
        // Initialize progression
        var persistence = new PlayerPrefsLevelPersistence();
        int totalLevels = LevelParser.GetTotalLevelCount();
        _progression = new LevelProgressionManager(persistence, totalLevels);

        // Initialize fail popup
        if (_failPopup != null)
            _failPopup.Initialize(this);

        // Load level
        int levelToPlay = _progression.GetLevelToPlay();
        if (levelToPlay == -1)
        {
            Debug.Log("[GameOrchestrator] All levels complete. Loading level 1 for testing.");
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

        // Build visual board
        _boardView.Initialize(_session.Board);
        _boardView.UpdateRocketHints(_session.GetHints());

        // Initialize UI
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
            // 1. Animate all steps
            await AnimateTurnSteps(turnResult);

            // 2. Update hints
            _boardView.UpdateRocketHints(turnResult.HintData);

            // 3. Update UI
            if (_gameplayUI != null)
            {
                _gameplayUI.UpdateMoves(turnResult.MovesRemaining);
                _gameplayUI.UpdateGoals(_session.GoalTracker);
            }

            // 4. Notify listeners
            OnMovesChanged?.Invoke(turnResult.MovesRemaining);
            OnGoalsUpdated?.Invoke(_session.GoalTracker);

            // 5. Handle win/lose
            HandleGameState(turnResult.StateAfterTurn);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameOrchestrator] Exception during turn: {ex}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    // ==========================================
    // STEP ANIMATION DISPATCHER
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
                    await AnimateComboExplosions(step.ComboExplosionData);
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

    private async Task AnimateComboExplosions(List<RocketExplosionData> explosions)
    {
        List<Task> tasks = new List<Task>();
        foreach (var data in explosions)
            tasks.Add(_boardView.AnimateRocketExplosion(data));
        await Task.WhenAll(tasks);
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
                else
                    Debug.LogWarning("[GameOrchestrator] No CelebrationUI assigned.");
                break;

            case GameSessionState.Lost:
                Debug.Log("[GameOrchestrator] LEVEL FAILED!");
                OnLevelFailed?.Invoke();

                if (_failPopup != null)
                    _failPopup.Show();
                else
                    Debug.LogWarning("[GameOrchestrator] No FailPopupUI assigned.");
                break;
        }
    }
}
