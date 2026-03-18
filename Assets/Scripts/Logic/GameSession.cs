using System.Collections.Generic;

/// <summary>
/// Pure C# state machine for playing one level.
/// Extracted from GameOrchestrator so it's fully NUnit-testable.
///
/// Usage:
///   var session = new GameSession(levelData);
///   TurnResult result = session.ProcessTap(coord);
///   // View animates result.Steps in order
///   // Check result.StateAfterTurn for win/lose
///
/// The GameSession owns ALL game state. The Orchestrator becomes a thin
/// wrapper that passes taps in and animates the TurnResult out.
/// </summary>
public class GameSession
{
    // --- Core systems ---
    private Board _board;
    private MatchStrategy _matchStrategy;
    private GravityProcessor _gravityProcessor;
    private RocketProcessor _rocketProcessor;
    private TapResolver _tapResolver;
    private ObstacleGoalTracker _goalTracker;

    // --- State ---
    private int _moveCount;
    private GameSessionState _state;

    private const int MaxChainDepth = 200;

    // --- Public read-only accessors ---
    public Board Board => _board;
    public int MovesRemaining => _moveCount;
    public GameSessionState State => _state;
    public ObstacleGoalTracker GoalTracker => _goalTracker;

    // ==========================================
    // CONSTRUCTION
    // ==========================================

    /// <summary>
    /// Create a session from parsed level data.
    /// </summary>
    public GameSession(LevelData levelData)
    {
        _board = new Board(levelData.grid_width, levelData.grid_height);
        _board.Initialize(levelData);
        _moveCount = levelData.move_count;
        InitializeSystems();
    }

    /// <summary>
    /// Create a session with a pre-built board (for testing).
    /// </summary>
    public GameSession(Board board, int moveCount)
    {
        _board = board;
        _moveCount = moveCount;
        InitializeSystems();
    }

    /// <summary>
    /// Create a session with an injected RocketProcessor (for deterministic testing).
    /// </summary>
    public GameSession(Board board, int moveCount, RocketProcessor rocketProcessor)
    {
        _board = board;
        _moveCount = moveCount;
        _matchStrategy = new ClassicMatchStrategy();
        _gravityProcessor = new GravityProcessor();
        _rocketProcessor = rocketProcessor;
        _tapResolver = new TapResolver();
        _goalTracker = new ObstacleGoalTracker();
        _goalTracker.InitializeFromBoard(_board);
        _state = GameSessionState.Playing;
    }

    private void InitializeSystems()
    {
        _matchStrategy = new ClassicMatchStrategy();
        _gravityProcessor = new GravityProcessor();
        _rocketProcessor = new RocketProcessor();
        _tapResolver = new TapResolver();
        _goalTracker = new ObstacleGoalTracker();
        _goalTracker.InitializeFromBoard(_board);
        _state = GameSessionState.Playing;
    }

    // ==========================================
    // MAIN ENTRY POINT
    // ==========================================

    /// <summary>
    /// Process a player tap at the given coordinate.
    /// Returns a TurnResult describing everything that happened.
    /// Returns TurnResult.Invalid() if the tap has no effect (no move spent).
    /// </summary>
    public TurnResult ProcessTap(Coordinate coord)
    {
        if (_state != GameSessionState.Playing || _moveCount <= 0)
            return TurnResult.Invalid();

        TapResult tapResult = _tapResolver.Resolve(_board, coord, _matchStrategy);

        switch (tapResult.Action)
        {
            case TapAction.BlastGroup:
                return HandleBlastGroup(tapResult);
            case TapAction.ExplodeRocket:
                return HandleExplodeRocket(tapResult);
            case TapAction.RocketCombo:
                return HandleRocketCombo(tapResult);
            default:
                return TurnResult.Invalid();
        }
    }

    /// <summary>
    /// Get current hint data (which cells should show rocket overlay).
    /// Call after construction and after each ProcessTap.
    /// </summary>
    public Dictionary<Coordinate, string> GetHints()
    {
        return HintCalculator.FindRocketHints(_board);
    }

    // ==========================================
    // BLAST GROUP
    // ==========================================

    private TurnResult HandleBlastGroup(TapResult tapResult)
    {
        _moveCount--;

        TurnResult result = new TurnResult { IsValid = true };
        List<Coordinate> matches = tapResult.MatchedCoordinates;
        Coordinate tapped = tapResult.TappedCoord;
        bool createsRocket = _rocketProcessor.QualifiesForRocket(matches.Count);

        // 1. Blast cubes + adjacent obstacle damage
        BlastResult blastResult = _matchStrategy.Blast(_board, matches, tapped);
        UpdateGoalTracker(blastResult.DestroyedObstacleInfos);

        // 2. Emit blast step
        if (createsRocket)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.BlastForRocket,
                BlastData = blastResult
            });

            // 3. Create rocket on board
            RocketCreationData rocketData = _rocketProcessor.CreateRocket(_board, tapped, matches);
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.RocketCreated,
                RocketCreationData = rocketData
            });
        }
        else
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.Blast,
                BlastData = blastResult
            });
        }

        // 4. Damaged sprites update
        if (blastResult.DamagedObstacles.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.UpdateDamagedSprites,
                DamagedSpritesToUpdate = blastResult.DamagedObstacles
            });
        }

        // 5. Settle board
        AppendSettleSteps(result);

        // 6. Final state
        result.StateAfterTurn = _state;
        result.MovesRemaining = _moveCount;
        result.HintData = GetHints();

        return result;
    }

    // ==========================================
    // SINGLE ROCKET EXPLOSION
    // ==========================================

    private TurnResult HandleExplodeRocket(TapResult tapResult)
    {
        _moveCount--;

        TurnResult result = new TurnResult { IsValid = true };

        // Queue-based chain reaction
        Queue<Coordinate> pending = new Queue<Coordinate>();
        pending.Enqueue(tapResult.TappedCoord);

        ProcessChainReactions(pending, result);

        // Settle board
        AppendSettleSteps(result);

        result.StateAfterTurn = _state;
        result.MovesRemaining = _moveCount;
        result.HintData = GetHints();

        return result;
    }

    // ==========================================
    // ROCKET COMBO
    // ==========================================

    private TurnResult HandleRocketCombo(TapResult tapResult)
    {
        _moveCount--;

        TurnResult result = new TurnResult { IsValid = true };

        // 1. Process combo logic
        List<RocketExplosionData> comboExplosions = _rocketProcessor.ProcessCombo(
            _board, tapResult.TappedCoord, tapResult.AdjacentRockets);

        // 2. Emit combo step (all explosions animated in parallel)
        Queue<Coordinate> pendingChains = new Queue<Coordinate>();

        foreach (var explosionData in comboExplosions)
        {
            UpdateGoalTracker(explosionData.DestroyedObstacleInfos);
            foreach (var triggered in explosionData.TriggeredRockets)
                pendingChains.Enqueue(triggered);
        }

        result.Steps.Add(new TurnStep
        {
            Type = TurnStepType.ComboExplosion,
            ComboExplosionData = comboExplosions
        });

        // 3. Damaged sprites from combo
        List<Coordinate> allDamaged = new List<Coordinate>();
        foreach (var exp in comboExplosions)
            allDamaged.AddRange(exp.DamagedObstacles);

        if (allDamaged.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.UpdateDamagedSprites,
                DamagedSpritesToUpdate = allDamaged
            });
        }

        // 4. Chain reactions from combo
        ProcessChainReactions(pendingChains, result);

        // 5. Settle board
        AppendSettleSteps(result);

        result.StateAfterTurn = _state;
        result.MovesRemaining = _moveCount;
        result.HintData = GetHints();

        return result;
    }

    // ==========================================
    // CHAIN REACTION QUEUE
    // ==========================================

    private void ProcessChainReactions(Queue<Coordinate> pending, TurnResult result)
    {
        int chainCount = 0;

        while (pending.Count > 0 && chainCount < MaxChainDepth)
        {
            Coordinate rocketCoord = pending.Dequeue();
            chainCount++;

            RocketExplosionData explosionData = _rocketProcessor.ExplodeRocket(_board, rocketCoord);
            if (explosionData == null)
                continue;

            UpdateGoalTracker(explosionData.DestroyedObstacleInfos);

            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.RocketExplosion,
                ExplosionData = explosionData
            });

            if (explosionData.DamagedObstacles.Count > 0)
            {
                result.Steps.Add(new TurnStep
                {
                    Type = TurnStepType.UpdateDamagedSprites,
                    DamagedSpritesToUpdate = explosionData.DamagedObstacles
                });
            }

            foreach (var triggered in explosionData.TriggeredRockets)
                pending.Enqueue(triggered);
        }
    }

    // ==========================================
    // BOARD SETTLING (Gravity + Refill + Win/Lose check)
    // ==========================================

    private void AppendSettleSteps(TurnResult result)
    {
        // Gravity
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        if (gravityMovements.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.Gravity,
                GravityData = gravityMovements
            });
        }

        // Refill
        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        if (refillMovements.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.Refill,
                RefillData = refillMovements
            });
        }

        // Check win/lose
        CheckGameState();
    }

    // ==========================================
    // STATE
    // ==========================================

    private void CheckGameState()
    {
        if (_board.AreAllObstaclesCleared())
        {
            _state = GameSessionState.Won;
            return;
        }

        if (_moveCount <= 0)
        {
            _state = GameSessionState.Lost;
        }
    }

    private void UpdateGoalTracker(List<DestroyedObstacleInfo> infos)
    {
        foreach (var info in infos)
            _goalTracker.OnObstacleDestroyed(info.ObstacleId);
    }
}
