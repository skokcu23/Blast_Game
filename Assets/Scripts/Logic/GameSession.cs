using System.Collections.Generic;

/// <summary>
/// Pure C# state machine for playing one level.
///
/// Orchestrates all game systems: match finding, blasting, rocket creation/explosion,
/// gravity, refill, chain reactions, goal tracking, and win/lose detection.
///
/// ProcessTap() is the single entry point. It returns a TurnResult containing
/// ordered steps for the View to animate. The View never calls game systems directly.
/// </summary>
public class GameSession
{
    private Board _board;
    private MatchStrategy _matchStrategy;
    private GravityProcessor _gravityProcessor;
    private RocketProcessor _rocketProcessor;
    private TapResolver _tapResolver;
    private ObstacleGoalTracker _goalTracker;

    private int _moveCount;
    private GameSessionState _state;

    private const int MaxChainDepth = 200;

    public Board Board => _board;
    public int MovesRemaining => _moveCount;
    public GameSessionState State => _state;
    public ObstacleGoalTracker GoalTracker => _goalTracker;

    // ==========================================
    // CONSTRUCTION
    // ==========================================

    /// <summary>Create a session from a parsed level file.</summary>
    public GameSession(LevelData levelData)
    {
        _board = new Board(levelData.grid_width, levelData.grid_height);
        _board.Initialize(levelData);
        _moveCount = levelData.move_count;
        InitializeSystems();
    }

    /// <summary>Create a session from a pre-built board (for testing).</summary>
    public GameSession(Board board, int moveCount)
    {
        _board = board;
        _moveCount = moveCount;
        InitializeSystems();
    }

    /// <summary>Create a session with a custom RocketProcessor (for seeded testing).</summary>
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
    /// Returns a TurnResult with ordered animation steps, or Invalid if no action.
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
    /// Get current rocket hint data (groups of 4+ same-color cubes).
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

        BlastResult blastResult = _matchStrategy.Blast(_board, matches, tapped);
        UpdateGoalTracker(blastResult.DestroyedObstacleInfos);

        if (createsRocket)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.BlastForRocket,
                BlastData = blastResult
            });

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

        AppendSettleSteps(result);

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

        Queue<Coordinate> pending = new Queue<Coordinate>();
        pending.Enqueue(tapResult.TappedCoord);

        ProcessChainReactions(pending, result);

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

        List<RocketExplosionData> comboExplosions = _rocketProcessor.ProcessCombo(
            _board, tapResult.TappedCoord, tapResult.AdjacentRockets);

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

        ProcessChainReactions(pendingChains, result);

        AppendSettleSteps(result);

        result.StateAfterTurn = _state;
        result.MovesRemaining = _moveCount;
        result.HintData = GetHints();

        return result;
    }

    // ==========================================
    // CHAIN REACTION QUEUE
    // ==========================================

    /// <summary>
    /// Process a queue of triggered rockets. Each explosion may trigger more rockets.
    /// Capped at MaxChainDepth to prevent infinite loops from board edge cases.
    /// </summary>
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

            foreach (var triggered in explosionData.TriggeredRockets)
                pending.Enqueue(triggered);
        }
    }

    // ==========================================
    // BOARD SETTLING
    // ==========================================

    /// <summary>
    /// Apply gravity + refill after any destructive action. Checks win/lose afterwards.
    /// </summary>
    private void AppendSettleSteps(TurnResult result)
    {
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        if (gravityMovements.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.Gravity,
                GravityData = gravityMovements
            });
        }

        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        if (refillMovements.Count > 0)
        {
            result.Steps.Add(new TurnStep
            {
                Type = TurnStepType.Refill,
                RefillData = refillMovements
            });
        }

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
