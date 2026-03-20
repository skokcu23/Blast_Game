using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single owner of the visual cell registry.
///
/// Damage visuals: AnimationController calls CubeView.ApplyDamageVisual directly
/// using pre-gravity DamagedObstacles coordinates. No manager involvement needed.
/// SyncWithBoard catches any remaining health mismatches as a safety net.
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
    // SYNC WITH BOARD (Safety Net)
    // ==========================================

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

        // Safety net: fix vase sprites based on actual health
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
}
