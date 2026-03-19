using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Purely cosmetic animation layer.
///
/// RULE: Never modifies GridStateManager._cells directly.
/// State changes happen through GridStateManager's API in OnComplete callbacks.
///
/// Creates temporary GameObjects (projectiles) that are NOT in the cell registry.
/// All DOTween animations with proper easing for satisfying game feel.
/// </summary>
public class AnimationController
{
    private readonly GridStateManager _gridState;
    private readonly BoardView _boardView;
    private readonly Transform _parent;

    // --- Timing Constants ---
    private const float POP_DURATION = 0.15f;
    private const float MERGE_DURATION = 0.2f;
    private const float GRAVITY_DURATION = 0.25f;
    private const float REFILL_DURATION = 0.35f;
    private const float SHAKE_DURATION = 0.3f;
    private const float SHAKE_STRENGTH = 0.1f;
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
    // BLAST (< 4 cubes, normal pop)
    // ==========================================

    public async Task PlayBlast(BlastResult result)
    {
        Sequence seq = DOTween.Sequence();

        // Pop blasted cubes
        foreach (var coord in result.BlastedCoordinates)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        // Pop destroyed obstacles
        foreach (var coord in result.DestroyedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        // Shake damaged obstacles
        foreach (var coord in result.DamagedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreateShakeTween(view));
        }

        await seq.ToTask();
    }

    // ==========================================
    // ROCKET CREATION (cubes merge toward center)
    // ==========================================

    public async Task PlayRocketCreation(BlastResult blastResult)
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
                // Shrink in place
                seq.Join(view.transform
                    .DOScale(Vector3.zero, MERGE_DURATION)
                    .SetEase(MERGE_EASE)
                    .OnComplete(() => _gridState.RemoveCell(capturedCoord)));
            }
            else
            {
                // Slide toward tapped cell and shrink
                seq.Join(view.transform
                    .DOMove(targetPos, MERGE_DURATION)
                    .SetEase(MERGE_EASE));
                seq.Join(view.transform
                    .DOScale(Vector3.zero, MERGE_DURATION)
                    .SetEase(MERGE_EASE)
                    .OnComplete(() => _gridState.RemoveCell(capturedCoord)));
            }
        }

        // Pop destroyed obstacles
        foreach (var coord in blastResult.DestroyedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreatePopTween(view, coord));
        }

        // Shake damaged obstacles
        foreach (var coord in blastResult.DamagedObstacles)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view != null)
                seq.Join(CreateShakeTween(view));
        }

        await seq.ToTask();
    }

    // ==========================================
    // ROCKET SPAWN (pop-in after creation)
    // ==========================================

    public void PlayRocketSpawn(RocketCreationData data)
    {
        if (data == null) return;

        // Place rocket in grid state
        CubeView rocketView = _gridState.PlaceCell(data.SpawnPosition, data.RocketId);

        // Pop-in animation
        rocketView.transform.localScale = Vector3.zero;
        rocketView.transform
            .DOScale(BoardView.CellScale, ROCKET_SPAWN_DURATION)
            .SetEase(Ease.OutBack);
    }

    // ==========================================
    // ROCKET EXPLOSION (single and combo)
    // ==========================================

    public async Task PlayRocketExplosion(RocketExplosionData data)
    {
        // 1. Remove rocket at origin
        _gridState.RemoveCell(data.Origin);

        // 1b. For combos: remove visuals that the logic actually destroyed
        if (data.IsCombo)
        {
            // Remove the origin (tapped rocket)
            _gridState.RemoveCell(data.Origin);

            // Remove destroyed cubes in 3×3 area
            foreach (var coord in data.DestroyedCubes)
                _gridState.RemoveCell(coord);

            // Remove destroyed obstacles in 3×3 area
            foreach (var coord in data.DestroyedObstacles)
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

        // 4. Direction vectors for fly-off
        int dirAx, dirAy, dirBx, dirBy;
        if (data.IsHorizontal)
        { dirAx = -1; dirAy = 0; dirBx = 1; dirBy = 0; }
        else
        { dirAx = 0; dirAy = -1; dirBx = 0; dirBy = 1; }

        // 5. Animate based on mode
        if (data.IsCombo)
            await AnimateComboProjectiles(data, partASprite, partBSprite, allDestroyed, dirAx, dirAy, dirBx, dirBy);
        else
            await AnimateSingleProjectiles(data, partASprite, partBSprite, allDestroyed, dirAx, dirAy, dirBx, dirBy);

        // 6. Cleanup remaining destroyed cells
        foreach (var coord in allDestroyed)
            _gridState.RemoveCell(coord);

        // 7. Shake damaged obstacles
        if (data.DamagedObstacles.Count > 0)
        {
            Sequence shakeSeq = DOTween.Sequence();
            foreach (var coord in data.DamagedObstacles)
            {
                CubeView view = _gridState.GetCell(coord);
                if (view != null)
                    shakeSeq.Join(CreateShakeTween(view));
            }
            await shakeSeq.ToTask();
        }
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

        // 3 projectiles going A direction (left/down)
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

        // 3 projectiles going B direction (right/up)
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

        // Fly off: one more unit past the edge
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
    // GRAVITY
    // ==========================================

    public async Task PlayGravity(List<ItemMovement> movements)
    {
        Sequence seq = DOTween.Sequence();

        foreach (var move in movements)
        {
            CubeView view = _gridState.GetCell(move.StartPos);
            if (view == null) continue;

            // Update state IMMEDIATELY — dict key moves to new position
            _gridState.MoveCell(move.StartPos, move.EndPos);

            // Animate visual position
            Vector3 endPos = _boardView.GridToWorld(move.EndPos);
            seq.Join(view.transform
                .DOMove(endPos, GRAVITY_DURATION)
                .SetEase(GRAVITY_EASE));
        }

        await seq.ToTask();
    }

    // ==========================================
    // REFILL
    // ==========================================

    public async Task PlayRefill(List<ItemMovement> newItems)
    {
        Sequence seq = DOTween.Sequence();

        foreach (var move in newItems)
        {
            // Place in grid state at FINAL position
            CubeView view = _gridState.PlaceCell(move.EndPos, move.ItemId);

            // Override visual position to SPAWN position (above board)
            view.transform.position = _boardView.GridToWorld(move.StartPos);

            // Animate to final position
            Vector3 endPos = _boardView.GridToWorld(move.EndPos);
            seq.Join(view.transform
                .DOMove(endPos, REFILL_DURATION)
                .SetEase(REFILL_EASE));
        }

        await seq.ToTask();
    }

    // ==========================================
    // DAMAGED SPRITES (vase cracking)
    // ==========================================

    public void PlayDamagedSprites(Board board, List<Coordinate> damagedCoords)
    {
        foreach (var coord in damagedCoords)
        {
            CubeView view = _gridState.GetCell(coord);
            if (view == null) continue;

            GridItem item = board.GetItem(coord);
            if (item.Id == ItemIds.Vase)
            {
                Sprite newSprite = _boardView.GetVaseSprite(item.Health);

                // Squash animation — swap sprite at the squeeze peak
                Sequence seq = DOTween.Sequence();

                seq.Append(view.transform
                    .DOScale(new Vector3(0.75f, 1.15f, 1f), 0.08f)
                    .SetEase(Ease.OutQuad));

                CubeView captured = view;
                Coordinate capturedCoord = coord;
                seq.AppendCallback(() =>
                {
                    if (captured != null)
                        _gridState.UpdateSprite(capturedCoord, newSprite);
                });

                seq.Append(view.transform
                    .DOScale(new Vector3(1.05f, 0.9f, 1f), 0.08f)
                    .SetEase(Ease.OutQuad));

                seq.Append(view.transform
                    .DOScale(BoardView.CellScale, 0.12f)
                    .SetEase(Ease.OutBounce));

                seq.Join(view.transform
                    .DOShakePosition(0.15f, 0.06f, 8, 90, false, true, ShakeRandomnessMode.Harmonic));
            }
        }
    }

    // ==========================================
    // TWEEN FACTORIES
    // ==========================================

    /// <summary>
    /// Pop: scale up slightly, then shrink to zero, then remove from grid state.
    /// </summary>
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

    /// <summary>
    /// Shake: quick horizontal vibration for obstacles taking damage.
    /// Does NOT remove — the obstacle survives.
    /// </summary>
    private Tween CreateShakeTween(CubeView view)
    {
        return view.transform
            .DOShakePosition(SHAKE_DURATION, SHAKE_STRENGTH, 10, 90, false, true, ShakeRandomnessMode.Harmonic)
            .SetEase(Ease.OutQuad);
    }
}
