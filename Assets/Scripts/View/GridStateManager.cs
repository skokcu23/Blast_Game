using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single owner of the visual cell registry.
///
/// Layer separation: animation methods receive only the data they need
/// (Dictionary of obstacle healths), never the full Board object.
/// Only SyncWithBoard (safety net) receives Board — it's called by
/// the Orchestrator, which is the designated mediator between layers.
/// </summary>
public class GridStateManager
{
    private readonly Dictionary<Coordinate, CubeView> _cells = new Dictionary<Coordinate, CubeView>();
    private readonly CubePool _pool;
    private readonly BoardView _boardView;

    public GridStateManager(CubePool pool, BoardView boardView)
    {
        _pool = pool;
        _boardView = boardView;
    }

    // ==========================================
    // CELL STATE CHANGES
    // ==========================================

    public CubeView PlaceCell(Coordinate coord, string itemId)
    {
        RemoveCell(coord);

        CubeView view = _pool.Get();
        Sprite sprite = _boardView.GetSpriteForItem(itemId);
        view.Configure(coord, sprite, itemId);
        view.transform.position = _boardView.GridToWorld(coord);
        view.transform.localScale = BoardView.CellScale;

        _cells[coord] = view;
        return view;
    }

    public CubeView PlaceCellWithHealth(Coordinate coord, string itemId, int health, Sprite sprite)
    {
        RemoveCell(coord);

        CubeView view = _pool.Get();
        view.ConfigureWithHealth(coord, sprite, itemId, health);
        view.transform.position = _boardView.GridToWorld(coord);
        view.transform.localScale = BoardView.CellScale;

        _cells[coord] = view;
        return view;
    }

    public void RemoveCell(Coordinate coord)
    {
        if (_cells.TryGetValue(coord, out var view))
        {
            _cells.Remove(coord);
            if (view != null)
                _pool.Return(view);
        }
    }

    public void MoveCell(Coordinate from, Coordinate to)
    {
        if (!_cells.TryGetValue(from, out var view)) return;

        _cells.Remove(from);
        RemoveCell(to);

        _cells[to] = view;
        view.UpdateCoordinate(to);
    }

    public void UpdateSprite(Coordinate coord, Sprite sprite)
    {
        if (_cells.TryGetValue(coord, out var view) && view != null)
        {
            view.SetSprite(sprite);
            view.SetDefaultSprite(sprite);
        }
    }

    public CubeView GetCell(Coordinate coord)
    {
        _cells.TryGetValue(coord, out var view);
        return view;
    }

    public bool HasCell(Coordinate coord)
    {
        return _cells.TryGetValue(coord, out var view) && view != null;
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Clear()
    {
        _cells.Clear();
        _pool.ReturnAll();
    }

    /// <summary>
    /// Spawn visuals for all non-empty cells. Obstacles get health tracking.
    /// This is the ONLY place that reads Board directly — during initialization,
    /// called by BoardView.Initialize which is called by the Orchestrator.
    /// </summary>
    public void SpawnAll(Board board)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate coord = new Coordinate(x, y);
                GridItem item = board.GetItem(coord);
                if (item.IsEmpty) continue;

                if (item.IsObstacle)
                {
                    Sprite sprite = item.Id == ItemIds.Vase
                        ? _boardView.GetVaseSprite(item.Health)
                        : _boardView.GetSpriteForItem(item.Id);
                    PlaceCellWithHealth(coord, item.Id, item.Health, sprite);
                }
                else
                {
                    PlaceCell(coord, item.Id);
                }
            }
        }
    }

    // ==========================================
    // SYNC WITH BOARD (Safety Net — Orchestrator mediates)
    // ==========================================

    /// <summary>
    /// Safety net. Called by Orchestrator (the layer mediator) after every turn.
    /// This is the only animation-phase method that receives Board directly,
    /// because it needs full board access for comprehensive reconciliation.
    /// Logs warnings for any fix — warnings indicate animation bugs.
    /// </summary>
    public void SyncWithBoard(Board board)
    {
        List<Coordinate> toRemove = new List<Coordinate>();

        foreach (var kvp in _cells)
        {
            Coordinate coord = kvp.Key;
            CubeView view = kvp.Value;

            if (view == null)
            {
                toRemove.Add(coord);
                continue;
            }

            GridItem boardItem = board.GetItem(coord);

            if (boardItem.IsEmpty)
            {
                Debug.LogWarning($"[GridState] Orphan at {coord} — visual exists but board is empty");
                toRemove.Add(coord);
            }
            else if (view.ItemId != boardItem.Id)
            {
                Debug.LogWarning($"[GridState] Type mismatch at {coord} — visual={view.ItemId}, board={boardItem.Id}");
                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
            RemoveCell(coord);

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate coord = new Coordinate(x, y);
                GridItem item = board.GetItem(coord);

                if (item.IsEmpty) continue;

                if (!HasCell(coord))
                {
                    Debug.LogWarning($"[GridState] Missing visual at {coord} — spawning {item.Id}");
                    if (item.IsObstacle)
                    {
                        Sprite sprite = item.Id == ItemIds.Vase
                            ? _boardView.GetVaseSprite(item.Health)
                            : _boardView.GetSpriteForItem(item.Id);
                        PlaceCellWithHealth(coord, item.Id, item.Health, sprite);
                    }
                    else
                    {
                        PlaceCell(coord, item.Id);
                    }
                }
            }
        }

        // Final health refresh using snapshot
        var healthSnapshot = BuildHealthSnapshot(board);
        RefreshObstacleVisuals(healthSnapshot);
    }

    /// <summary>
    /// Build a health snapshot from the board. Used internally by SyncWithBoard.
    /// The Orchestrator builds its own snapshots for animation steps.
    /// </summary>
    private Dictionary<Coordinate, int> BuildHealthSnapshot(Board board)
    {
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
    // DERIVED STATE: Hints (Stateless, Idempotent)
    // ==========================================

    public void UpdateHints(Dictionary<Coordinate, string> newHints)
    {
        foreach (var kvp in _cells)
        {
            Coordinate coord = kvp.Key;
            CubeView view = kvp.Value;
            if (view == null) continue;

            if (newHints.TryGetValue(coord, out string colorId))
            {
                Sprite hintSprite = _boardView.GetHintSpriteForColor(colorId);
                view.ApplyHint(hintSprite);
            }
            else
            {
                view.RemoveHint();
            }
        }
    }

    // ==========================================
    // DERIVED STATE: Health Visuals (Stateless, Idempotent)
    // ==========================================

    /// <summary>
    /// Refresh obstacle visuals using a health snapshot.
    /// The snapshot is a Dictionary mapping Coordinate → current health
    /// for all living vases. Built by the Orchestrator from Board state.
    ///
    /// View layer never sees Board — only this lightweight data.
    /// </summary>
    public void RefreshObstacleVisuals(Dictionary<Coordinate, int> obstacleHealth)
    {
        foreach (var kvp in _cells)
        {
            CubeView view = kvp.Value;
            if (view == null) continue;
            if (view.ItemId != ItemIds.Vase) continue;

            if (obstacleHealth.TryGetValue(kvp.Key, out int health))
            {
                Sprite correctSprite = _boardView.GetVaseSprite(health);
                view.SetHealthVisual(health, correctSprite);
            }
        }
    }
}
