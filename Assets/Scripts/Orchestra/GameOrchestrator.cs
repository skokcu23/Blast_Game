using System.Threading.Tasks;
using UnityEngine;

public class GameOrchestrator : MonoBehaviour
{
    [SerializeField]
    private BoardView _boardView;

    [SerializeField]
    private InputManager _inputManager; // New Reference!
    private Board _board;
    private MatchStrategy _matchStrategy;
    private int _moveCount = 25; // From level data in future iterations [cite: 156]
    private bool _isBusy;

    // --- Event Subscription ---
    private void OnEnable()
    {
        if (_inputManager != null)
            _inputManager.OnCubeTapped += OnCellTapped; // Start listening
    }

    private void OnDisable()
    {
        if (_inputManager != null)
            _inputManager.OnCubeTapped -= OnCellTapped; // Stop listening
    }

    // --------------------------
    void Start()
    {
        // 1. Initialize Logic Layer
        _board = new Board(8, 10);
        _board.Initialize();
        _matchStrategy = new ClassicMatchStrategy();

        // 2. Initialize View Layer
        // FIX: Pass 'this' so the BoardView knows who the Orchestrator is
        _boardView.Initialize(_board);
    }

    public async void OnCellTapped(Coordinate coord)
    {
        if (_isBusy || _moveCount <= 0)
            return;

        // 1. Logic Check (Instant)
        var matches = _matchStrategy.findMatches(_board, coord);

        if (matches.Count >= 2)
        { // "Match-2" rule
            _isBusy = true;
            _moveCount--; // Spend move

            // 2. Update Logic State
            _matchStrategy.Blast(_board, matches);

            // 3. Sync View (Time-Taken)
            // Tell the puppet to play the "Pop" animation
            await _boardView.AnimateBlast(matches);

            // In Iteration 2, we would call Fall/Refill here

            _isBusy = false;
        }
    }
}
