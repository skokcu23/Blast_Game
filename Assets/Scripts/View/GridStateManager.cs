using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single owner of the visual cell registry.
///
/// RULE: No other class modifies _cells. AnimationController plays tweens
/// on CubeViews, then calls back into this class to update state.
///
/// Hint system is STATELESS — no coordinate tracking.
/// Each update walks all cells and tells each one its current hint state.
/// CubeViews are idempotent (ApplyHint/RemoveHint are no-ops if already in target state).
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

    /// <summary>
    /// Get a CubeView from the pool, configure it, position it, add to registry.
    /// </summary>
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

    /// <summary>
    /// Remove a CubeView from the registry and return it to the pool.
    /// </summary>
    public void RemoveCell(Coordinate coord)
    {
        if (_cells.TryGetValue(coord, out var view))
        {
            _cells.Remove(coord);
            if (view != null)
                _pool.Return(view);
        }
    }

    /// <summary>
    /// Move a CubeView from one coordinate to another in the registry.
    /// Only updates dict key + CubeView coordinate. Visual position is animated separately.
    /// </summary>
    public void MoveCell(Coordinate from, Coordinate to)
    {
        if (!_cells.TryGetValue(from, out var view)) return;

        _cells.Remove(from);
        RemoveCell(to);

        _cells[to] = view;
        view.UpdateCoordinate(to);
    }

    /// <summary>
    /// Update the sprite of an existing cell (e.g., vase cracking).
    /// </summary>
    public void UpdateSprite(Coordinate coord, Sprite sprite)
    {
        if (_cells.TryGetValue(coord, out var view) && view != null)
        {
            view.SetSprite(sprite);
            view.SetDefaultSprite(sprite);
        }
    }

    /// <summary>
    /// Look up a CubeView by coordinate. Returns null if not found.
    /// </summary>
    public CubeView GetCell(Coordinate coord)
    {
        _cells.TryGetValue(coord, out var view);
        return view;
    }

    /// <summary>
    /// Check if a visual exists at a coordinate.
    /// </summary>
    public bool HasCell(Coordinate coord)
    {
        return _cells.TryGetValue(coord, out var view) && view != null;
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    /// <summary>
    /// Clear all visuals and return everything to the pool.
    /// </summary>
    public void Clear()
    {
        _cells.Clear();
        _pool.ReturnAll();
    }

    /// <summary>
    /// Spawn visuals for all non-empty cells on the board.
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
                PlaceCell(coord, item.Id);
            }
        }
    }

    // ==========================================
    // SYNC WITH BOARD (Safety Net)
    // ==========================================

    /// <summary>
    /// Walk the entire board and fix any mismatch.
    /// Logs warnings for every fix — if you see warnings, an animation has a bug.
    /// </summary>
    public void SyncWithBoard(Board board)
    {
        // 1. Remove orphans
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

        // 2. Spawn missing
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
                    PlaceCell(coord, item.Id);
                }
            }
        }

        // 3. Fix vase sprites based on health
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate coord = new Coordinate(x, y);
                GridItem item = board.GetItem(coord);

                if (item.Id == ItemIds.Vase && _cells.TryGetValue(coord, out var view) && view != null)
                {
                    Sprite correctSprite = _boardView.GetVaseSprite(item.Health);
                    view.SetSprite(correctSprite);
                    view.SetDefaultSprite(correctSprite);
                }
            }
        }
    }

    // ==========================================
    // HINTS (Stateless — No Coordinate Tracking)
    // ==========================================

    /// <summary>
    /// Update hints across all active cells.
    ///
    /// Design: STATELESS. No tracking of previous hint coordinates.
    /// Walks every active cell and tells it the current state.
    /// CubeView.ApplyHint/RemoveHint are idempotent — no-op if
    /// already in the target state. No duplicate animations.
    ///
    /// Immune to gravity because we iterate by current dict keys,
    /// not by remembered old coordinates.
    /// </summary>
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
}