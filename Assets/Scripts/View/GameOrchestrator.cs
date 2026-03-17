using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Controller: bridges Logic and View layers.
/// Listens for input → asks Logic to calculate → awaits View animations → checks win/lose.
/// </summary>
public class GameOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BoardView _boardView;

    [SerializeField]
    private InputManager _inputManager;

    // Logic layer (pure C#, no MonoBehaviour)
    private Board _board;
    private MatchStrategy _matchStrategy;
    private GravityProcessor _gravityProcessor;
    private LevelData _currentLevel;

    // State
    private int _moveCount;
    private bool _isBusy;

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
        // For now, hardcode level 1 for testing
        LoadLevel(1);
    }

    /// <summary>
    /// Initialize everything for a given level number.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        // 1. Parse level data
        _currentLevel = LevelParser.LoadLevel(levelNumber);

        if (_currentLevel == null)
        {
            Debug.LogError(
                $"[GameOrchestrator] Failed to load level {levelNumber}. "
                    + "Falling back to random 8x10 board."
            );
            _board = new Board(8, 10);
            _board.InitializeRandom();
            _moveCount = 25;
        }
        else
        {
            // 2. Build logic board from level data
            _board = new Board(_currentLevel.grid_width, _currentLevel.grid_height);
            _board.Initialize(_currentLevel);
            _moveCount = _currentLevel.move_count;
        }

        // 3. Initialize strategies
        _matchStrategy = new ClassicMatchStrategy();
        _gravityProcessor = new GravityProcessor();

        // 4. Build the visual board
        _boardView.Initialize(_board);

        _isBusy = false;

        Debug.Log(
            $"[GameOrchestrator] Level {levelNumber} loaded. "
                + $"Grid: {_board.Width}x{_board.Height}, Moves: {_moveCount}"
        );
    }

    public async void OnCellTapped(Coordinate coord)
    {
        if (_isBusy || _moveCount <= 0)
            return;

        // 1. Find matches (instant logic)
        var matches = _matchStrategy.FindMatches(_board, coord);

        if (matches.Count < 2)
            return; // No valid match — don't spend a move

        _isBusy = true;
        _moveCount--;

        // 2. Execute blast (logic: clears cubes, damages adjacent obstacles)
        BlastResult blastResult = _matchStrategy.Blast(_board, matches, coord);

        // 3. Animate the blast
        await _boardView.AnimateBlast(blastResult);

        // 4. Rocket creation (Iteration 3 — placeholder)
        if (blastResult.ShouldCreateRocket)
        {
            Debug.Log($"[GameOrchestrator] Rocket should spawn at {coord} (not yet implemented)");
            // TODO: Create rocket item at coord, animate cube-merge, etc.
        }

        // 5. Gravity
        var gravityMovements = _gravityProcessor.ApplyGravity(_board);
        await _boardView.AnimateGravity(gravityMovements);

        // 6. Refill
        var refillMovements = _gravityProcessor.FillEmptySpaces(_board);
        await _boardView.AnimateRefill(refillMovements);

        // 7. Check win/lose
        CheckGameState();

        _isBusy = false;
    }

    private void CheckGameState()
    {
        // Win: all obstacles cleared
        if (_board.AreAllObstaclesCleared())
        {
            Debug.Log("[GameOrchestrator] LEVEL WON!");
            // TODO (Iteration 4): Show celebration, load MainScene
            return;
        }

        // Lose: no moves left but obstacles remain
        if (_moveCount <= 0)
        {
            Debug.Log("[GameOrchestrator] LEVEL FAILED — no moves remaining.");
            // TODO (Iteration 4): Show fail popup
            return;
        }

        Debug.Log(
            $"[GameOrchestrator] Moves remaining: {_moveCount}, "
                + $"Obstacles left: {_board.CountObstacles()}"
        );
    }
}
