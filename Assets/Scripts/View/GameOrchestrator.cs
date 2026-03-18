using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Controller: bridges Logic and View layers.
///
/// Key design decisions (3B v2):
///   - try/finally around all handlers to prevent _isBusy deadlock on exceptions
///   - Combo explosions animated in parallel (Task.WhenAll)
///   - Shared ProcessChainReactions() used by both rocket tap and combo
///   - MaxChainDepth = 200 (covers a fully packed 10×10 board)
/// </summary>
public class GameOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardView _boardView;
    [SerializeField] private InputManager _inputManager;

    private Board _board;
    private MatchStrategy _matchStrategy;
    private GravityProcessor _gravityProcessor;
    private RocketProcessor _rocketProcessor;
    private TapResolver _tapResolver;
    private ObstacleGoalTracker _goalTracker;
    private LevelData _currentLevel;

    private int _moveCount;
    private bool _isBusy;
    private bool _gameOver;

    private const int MaxChainDepth = 200;

    public event Action<int> OnMovesChanged;
    public event Action<ObstacleGoalTracker> OnGoalsUpdated;
    public event Action OnLevelWon;
    public event Action OnLevelFailed;

    public int MovesRemaining => _moveCount;
    public ObstacleGoalTracker GoalTracker => _goalTracker;

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

    void Start() => LoadLevel(1);

    public void LoadLevel(int levelNumber)
    {
        _gameOver = false;
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

        _matchStrategy = new ClassicMatchStrategy();
        _gravityProcessor = new GravityProcessor();
        _rocketProcessor = new RocketProcessor();
        _tapResolver = new TapResolver();
        _goalTracker = new ObstacleGoalTracker();
        _goalTracker.InitializeFromBoard(_board);

        _boardView.Initialize(_board);
        _isBusy = false;

        RefreshHints();
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

        TapResult tapResult = _tapResolver.Resolve(_board, coord, _matchStrategy);

        // try/finally prevents _isBusy deadlock if any animation throws
        try
        {
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
                    return; // No move spent, no _isBusy set
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameOrchestrator] Exception during tap handling: {ex}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    // ==========================================
    // BLAST GROUP
    // ==========================================

    private async Task HandleBlastGroup(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        List<Coordinate> matches = tapResult.MatchedCoordinates;
        Coordinate tapped = tapResult.TappedCoord;
        bool createsRocket = _rocketProcessor.QualifiesForRocket(matches.Count);

        BlastResult blastResult = _matchStrategy.Blast(_board, matches, tapped);
        UpdateGoalTrackerFromBlast(blastResult);

        if (createsRocket)
        {
            await _boardView.AnimateRocketCreation(blastResult, tapped);
            RocketCreationData rocketData = _rocketProcessor.CreateRocket(_board, tapped, matches);
            _boardView.SpawnRocketVisual(rocketData);
        }
        else
        {
            await _boardView.AnimateBlast(blastResult);
        }

        _boardView.UpdateDamagedSprites(_board, blastResult.DamagedObstacles);

        await SettleBoard();
        CheckGameState();
    }

    // ==========================================
    // SINGLE ROCKET EXPLOSION + CHAIN REACTIONS
    // ==========================================

    private async Task HandleExplodeRocket(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        Queue<Coordinate> pending = new Queue<Coordinate>();
        pending.Enqueue(tapResult.TappedCoord);

        await ProcessChainReactions(pending);
        await SettleBoard();
        CheckGameState();
    }

    // ==========================================
    // ROCKET COMBO + CHAIN REACTIONS
    // ==========================================

    private async Task HandleRocketCombo(TapResult tapResult)
    {
        _isBusy = true;
        _moveCount--;
        OnMovesChanged?.Invoke(_moveCount);

        // 1. Process combo logic
        List<RocketExplosionData> comboExplosions = _rocketProcessor.ProcessCombo(
            _board, tapResult.TappedCoord, tapResult.AdjacentRockets);

        // 2. Animate ALL combo explosions in PARALLEL
        List<Task> animTasks = new List<Task>();
        Queue<Coordinate> pendingChains = new Queue<Coordinate>();

        foreach (var explosionData in comboExplosions)
        {
            UpdateGoalTrackerFromExplosion(explosionData);
            animTasks.Add(_boardView.AnimateRocketExplosion(explosionData));

            foreach (var triggered in explosionData.TriggeredRockets)
                pendingChains.Enqueue(triggered);
        }

        await Task.WhenAll(animTasks);

        // 3. Update damaged sprites after all combo anims complete
        foreach (var explosionData in comboExplosions)
            _boardView.UpdateDamagedSprites(_board, explosionData.DamagedObstacles);

        // 4. Process chain reactions from combo
        await ProcessChainReactions(pendingChains);

        await SettleBoard();
        CheckGameState();
    }

    // ==========================================
    // CHAIN REACTION QUEUE
    // ==========================================

    /// <summary>
    /// Queue-based chain reaction loop.
    ///
    /// Key invariant: Triggered rockets are still ON the board when enqueued.
    /// ExplodeRocket() removes them when it's their turn.
    /// If a rocket was already removed by a prior chain step, the IsRocket
    /// check safely skips it (returns null from ExplodeRocket).
    /// </summary>
    private async Task ProcessChainReactions(Queue<Coordinate> pending)
    {
        int chainCount = 0;

        while (pending.Count > 0 && chainCount < MaxChainDepth)
        {
            Coordinate rocketCoord = pending.Dequeue();
            chainCount++;

            // ExplodeRocket checks IsRocket internally — returns null if cell
            // is empty (already fired by a prior chain step) or not a rocket
            RocketExplosionData explosionData = _rocketProcessor.ExplodeRocket(_board, rocketCoord);
            if (explosionData == null)
                continue;

            UpdateGoalTrackerFromExplosion(explosionData);

            await _boardView.AnimateRocketExplosion(explosionData);
            _boardView.UpdateDamagedSprites(_board, explosionData.DamagedObstacles);

            foreach (var triggered in explosionData.TriggeredRockets)
                pending.Enqueue(triggered);
        }

        if (chainCount >= MaxChainDepth)
            Debug.LogWarning("[GameOrchestrator] Chain reaction hit safety limit!");
    }

    // ==========================================
    // SHARED
    // ==========================================

    private async Task SettleBoard()
    {
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        await _boardView.AnimateGravity(gravityMovements);

        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        await _boardView.AnimateRefill(refillMovements);

        RefreshHints();
    }

    private void RefreshHints()
    {
        var hintData = HintCalculator.FindRocketHints(_board);
        _boardView.UpdateRocketHints(hintData);
    }

    private void UpdateGoalTrackerFromBlast(BlastResult result)
    {
        foreach (var info in result.DestroyedObstacleInfos)
            _goalTracker.OnObstacleDestroyed(info.ObstacleId);
        OnGoalsUpdated?.Invoke(_goalTracker);
    }

    private void UpdateGoalTrackerFromExplosion(RocketExplosionData data)
    {
        foreach (var info in data.DestroyedObstacleInfos)
            _goalTracker.OnObstacleDestroyed(info.ObstacleId);
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
