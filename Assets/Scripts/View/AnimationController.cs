using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Purely cosmetic animation layer.
///
/// LAYER RULE: Never receives Board. Only receives view-layer data:
///   - BlastResult, RocketExplosionData, etc. (DTOs from logic, read-only)
///   - Dictionary&lt;Coordinate, int&gt; obstacleHealth (snapshot built by Orchestrator)
///
/// After each damage-causing step, calls RefreshObstacleVisuals with
/// the health snapshot. CubeView.SetHealthVisual handles the rest idempotently.
/// </summary>
public class AnimationController
{
    private readonly GridStateManager _gridState;
    private readonly BoardView _boardView;
    private readonly Transform _parent;

    private const float POP_DURATION = 0.15f;
    private const float MERGE_DURATION = 0.2f;
    private const float GRAVITY_DURATION = 0.25f;
    private const float REFILL_DURATION = 0.35f;
    private const float SHAKE_DURATION = 0.2f;
    private const float SHAKE_STRENGTH = 0.08f;
    private const float PROJECTILE_SPEED = 0.03f;
    private const float ROCKET_SPAWN_DURATION = 0.25f;

    private const Ease GRAVITY_EASE = Ease.OutBounce;
    private const Ease REFILL_EASE = Ease.OutQuad;
    private const Ease MERGE_EASE = Ease.InQuad;
    private const Ease POP_EASE = Ease.OutBack;

    public AnimationController(GridStateManager gridState, BoardView boardView, Transform parent)
    {
        _gridState = gridState;
        _boardView = boardView;
        _parent = parent;
    }

    // ==========================================
    // BLAST
    // ==========================================

    public async Task PlayBlast(BlastResult result, Dictionary<Coordinate, int> obstacleHealth)
    {
        Sequence seq = DOTween.Sequence();

        foreach (var coord in result.BlastedCoordinates)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        foreach (var coord in result.DestroyedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        foreach (var coord in result.DamagedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreateShakeTween(view));
        }

        await seq.ToTask();

        _gridState.RefreshObstacleVisuals(obstacleHealth);
    }

    // ==========================================
    // ROCKET CREATION
    // ==========================================

    public async Task PlayRocketCreation(BlastResult blastResult, Dictionary<Coordinate, int> obstacleHealth)
    {
        Coordinate tapped = blastResult.RocketSpawnPosition;
        Vector3 targetPos = _boardView.GridToWorld(tapped);
        Sequence seq = DOTween.Sequence();

        foreach (var coord in blastResult.BlastedCoordinates)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view == null) continue;

            Coordinate capturedCoord = coord;

            if (coord == tapped)
            {
                seq.Join(view.transform
                    .DOScale(Vector3.zero, MERGE_DURATION)
                    .SetEase(MERGE_EASE)
                    .OnComplete(() => _gridState.RemoveCell(capturedCoord)));
            }
            else
            {
                seq.Join(view.transform
                    .DOMove(targetPos, MERGE_DURATION)
                    .SetEase(MERGE_EASE));
                seq.Join(view.transform
                    .DOScale(Vector3.zero, MERGE_DURATION)
                    .SetEase(MERGE_EASE)
                    .OnComplete(() => _gridState.RemoveCell(capturedCoord)));
            }
        }

        foreach (var coord in blastResult.DestroyedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        foreach (var coord in blastResult.DamagedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreateShakeTween(view));
        }

        await seq.ToTask();

        _gridState.RefreshObstacleVisuals(obstacleHealth);
    }

    // ==========================================
    // ROCKET SPAWN
    // ==========================================

    public void PlayRocketSpawn(RocketCreationData data)
    {
        if (data == null) return;

        CubeView rocketView = _gridState.PlaceCell(data.SpawnPosition, data.RocketId);

        rocketView.transform.localScale = Vector3.zero;
        rocketView.transform
            .DOScale(BoardView.CellScale, ROCKET_SPAWN_DURATION)
            .SetEase(Ease.OutBack);
    }

    // ==========================================
    // ROCKET EXPLOSION
    // ==========================================

    public async Task PlayRocketExplosion(RocketExplosionData data, Dictionary<Coordinate, int> obstacleHealth)
    {
        // 1. Remove rocket at origin
        _gridState.RemoveCell(data.Origin);

        // 1b. Combo: clear visuals for emptied 3×3 cells
        if (data.IsCombo && data.ComboAreaCleared != null)
        {
            foreach (var coord in data.ComboAreaCleared)
                _gridState.RemoveCell(coord);
        }

        // 2. Build destroyed set
        HashSet<Coordinate> allDestroyed = new HashSet<Coordinate>();
        foreach (var c in data.DestroyedCubes) allDestroyed.Add(c);
        foreach (var c in data.DestroyedObstacles) allDestroyed.Add(c);
        foreach (var c in data.TriggeredRockets) allDestroyed.Add(c);

        // 3. Determine sprites
        Sprite partASprite, partBSprite;
        if (data.IsHorizontal)
        {
            partASprite = _boardView.HorizontalPartLeftSprite;
            partBSprite = _boardView.HorizontalPartRightSprite;
        }
        else
        {
            partASprite = _boardView.VerticalPartBottomSprite;
            partBSprite = _boardView.VerticalPartTopSprite;
        }

        // 4. Direction vectors
        int dirAx, dirAy, dirBx, dirBy;
        if (data.IsHorizontal)
        { dirAx = -1; dirAy = 0; dirBx = 1; dirBy = 0; }
        else
        { dirAx = 0; dirAy = -1; dirBx = 0; dirBy = 1; }

        // 5. Animate
        if (data.IsCombo)
            await AnimateComboProjectiles(data, partASprite, partBSprite, allDestroyed, dirAx, dirAy, dirBx, dirBy);
        else
            await AnimateSingleProjectiles(data, partASprite, partBSprite, allDestroyed, dirAx, dirAy, dirBx, dirBy);

        // 6. Cleanup remaining
        foreach (var coord in allDestroyed)
            _gridState.RemoveCell(coord);

        // 7. Refresh obstacle visuals
        _gridState.RefreshObstacleVisuals(obstacleHealth);
    }

    private async Task AnimateSingleProjectiles(
        RocketExplosionData data, Sprite spriteA, Sprite spriteB,
        HashSet<Coordinate> allDestroyed,
        int dirAx, int dirAy, int dirBx, int dirBy)
    {
        GameObject partA = CreateProjectile(data.Origin, spriteA);
        GameObject partB = CreateProjectile(data.Origin, spriteB);

        await Task.WhenAll(
            AnimateProjectilePath(partA, data.PathA, allDestroyed, dirAx, dirAy),
            AnimateProjectilePath(partB, data.PathB, allDestroyed, dirBx, dirBy)
        );

        if (partA != null) Object.Destroy(partA);
        if (partB != null) Object.Destroy(partB);
    }

    private async Task AnimateComboProjectiles(
        RocketExplosionData data, Sprite spriteA, Sprite spriteB,
        HashSet<Coordinate> allDestroyed,
        int dirAx, int dirAy, int dirBx, int dirBy)
    {
        List<GameObject> projectiles = new List<GameObject>();
        List<Task> tasks = new List<Task>();
        int[] offsets = { -1, 0, 1 };

        for (int i = 0; i < data.ParallelPathsA.Count && i < 3; i++)
        {
            if (data.ParallelPathsA[i].Count == 0) continue;

            Coordinate spawnCoord = data.IsHorizontal
                ? new Coordinate(data.Origin.x, data.Origin.y + offsets[i])
                : new Coordinate(data.Origin.x + offsets[i], data.Origin.y);

            GameObject proj = CreateProjectile(spawnCoord, spriteA);
            projectiles.Add(proj);
            tasks.Add(AnimateProjectilePath(proj, data.ParallelPathsA[i], allDestroyed, dirAx, dirAy));
        }

        for (int i = 0; i < data.ParallelPathsB.Count && i < 3; i++)
        {
            if (data.ParallelPathsB[i].Count == 0) continue;

            Coordinate spawnCoord = data.IsHorizontal
                ? new Coordinate(data.Origin.x, data.Origin.y + offsets[i])
                : new Coordinate(data.Origin.x + offsets[i], data.Origin.y);

            GameObject proj = CreateProjectile(spawnCoord, spriteB);
            projectiles.Add(proj);
            tasks.Add(AnimateProjectilePath(proj, data.ParallelPathsB[i], allDestroyed, dirBx, dirBy));
        }

        await Task.WhenAll(tasks);

        foreach (var proj in projectiles)
            if (proj != null) Object.Destroy(proj);
    }

    private async Task AnimateProjectilePath(
        GameObject projectile, List<Coordinate> path,
        HashSet<Coordinate> destroyedCoords, int dirX, int dirY)
    {
        if (projectile == null || path.Count == 0) return;

        for (int i = 0; i < path.Count; i++)
        {
            if (projectile == null) return;

            Coordinate cell = path[i];
            Vector3 targetPos = _boardView.GridToWorld(cell);

            await projectile.transform
                .DOMove(targetPos, PROJECTILE_SPEED)
                .SetEase(Ease.Linear)
                .ToTask();

            if (destroyedCoords.Contains(cell))
                _gridState.RemoveCell(cell);
        }

        if (projectile != null)
        {
            Coordinate lastCell = path[path.Count - 1];
            Vector3 flyOffPos = _boardView.GridToWorld(
                new Coordinate(lastCell.x + dirX, lastCell.y + dirY));

            await projectile.transform
                .DOMove(flyOffPos, PROJECTILE_SPEED)
                .SetEase(Ease.Linear)
                .ToTask();

            if (projectile != null)
                Object.Destroy(projectile);
        }
    }

    private GameObject CreateProjectile(Coordinate origin, Sprite sprite)
    {
        Vector3 pos = _boardView.GridToWorld(origin);
        GameObject go = new GameObject("RocketProjectile");
        go.transform.position = pos;
        go.transform.SetParent(_parent);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        go.transform.localScale = BoardView.CellScale;

        return go;
    }

    // ==========================================
    // GRAVITY (no damage — no refresh)
    // ==========================================

    public async Task PlayGravity(List<ItemMovement> movements)
    {
        Sequence seq = DOTween.Sequence();

        foreach (var move in movements)
        {
            CubeView view = _gridState.GetCell(move.StartPos);
            if (view == null) continue;

            _gridState.MoveCell(move.StartPos, move.EndPos);

            Vector3 endPos = _boardView.GridToWorld(move.EndPos);
            seq.Join(view.transform
                .DOMove(endPos, GRAVITY_DURATION)
                .SetEase(GRAVITY_EASE));
        }

        await seq.ToTask();
    }

    // ==========================================
    // REFILL (no damage — no refresh)
    // ==========================================

    public async Task PlayRefill(List<ItemMovement> newItems)
    {
        Sequence seq = DOTween.Sequence();

        foreach (var move in newItems)
        {
            CubeView view = _gridState.PlaceCell(move.EndPos, move.ItemId);

            view.transform.position = _boardView.GridToWorld(move.StartPos);

            Vector3 endPos = _boardView.GridToWorld(move.EndPos);
            seq.Join(view.transform
                .DOMove(endPos, REFILL_DURATION)
                .SetEase(REFILL_EASE));
        }

        await seq.ToTask();
    }

    // ==========================================
    // TWEEN FACTORIES
    // ==========================================

    private Tween CreatePopTween(CubeView view, Coordinate coord)
    {
        Coordinate capturedCoord = coord;

        return DOTween.Sequence()
            .Append(view.transform
                .DOScale(BoardView.CellScale * 1.3f, POP_DURATION * 0.4f)
                .SetEase(POP_EASE))
            .Append(view.transform
                .DOScale(Vector3.zero, POP_DURATION * 0.6f)
                .SetEase(Ease.InQuad))
            .OnComplete(() => _gridState.RemoveCell(capturedCoord));
    }

    private Tween CreateShakeTween(CubeView view)
    {
        return view.transform
            .DOShakePosition(SHAKE_DURATION, SHAKE_STRENGTH, 10, 90, false, true, ShakeRandomnessMode.Harmonic)
            .SetEase(Ease.OutQuad);
    }
}