using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The View layer: a puppet stage that visually reflects the Board's state.
///
/// Knows nothing about game rules. The Orchestrator iterates TurnResult.Steps
/// and calls the appropriate animation method for each step.
///
/// Bug fixes from 3B audit are baked in:
///   - DestroyCubeAt() for consistent cleanup
///   - TriggeredRockets in allDestroyed set
///   - Off-path cleanup pass after projectile animation
///   - Defensive destroy in AnimateGravity/AnimateRefill
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject _cubePrefab;

    [Header("Cube Sprites")]
    [SerializeField] private Sprite _blueSprite;
    [SerializeField] private Sprite _redSprite;
    [SerializeField] private Sprite _yellowSprite;
    [SerializeField] private Sprite _greenSprite;

    [Header("Rocket Hint Sprites")]
    [SerializeField] private Sprite _redRocketHint;
    [SerializeField] private Sprite _blueRocketHint;
    [SerializeField] private Sprite _greenRocketHint;
    [SerializeField] private Sprite _yellowRocketHint;

    [Header("Obstacle Sprites")]
    [SerializeField] private Sprite _boxSprite;
    [SerializeField] private Sprite _stoneSprite;
    [SerializeField] private Sprite _vaseSprite;
    [SerializeField] private Sprite _vaseDamagedSprite;

    [Header("Rocket Sprites")]
    [SerializeField] private Sprite _verticalRocketSprite;
    [SerializeField] private Sprite _horizontalRocketSprite;

    [Header("Rocket Part Sprites")]
    [SerializeField] private Sprite _horizontalPartLeftSprite;
    [SerializeField] private Sprite _horizontalPartRightSprite;
    [SerializeField] private Sprite _verticalPartTopSprite;
    [SerializeField] private Sprite _verticalPartBottomSprite;

    // --- Internal state ---
    private Dictionary<Coordinate, CubeView> _activeCubes = new Dictionary<Coordinate, CubeView>();
    private float _offsetX;
    private float _offsetY;
    private HashSet<Coordinate> _currentHintCoords = new HashSet<Coordinate>();

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(Board board)
    {
        // Clear previous level visuals
        foreach (var kvp in _activeCubes)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _activeCubes.Clear();
        _currentHintCoords.Clear();

        // Center the grid
        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;

        // Spawn visuals for all non-empty cells
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate coord = new Coordinate(x, y);
                GridItem item = board.GetItem(coord);
                if (item.IsEmpty) continue;
                SpawnCube(coord, item.Id);
            }
        }

        FitCameraToBoard(board.Width, board.Height);
    }

    private void FitCameraToBoard(int boardWidth, int boardHeight)
    {
        if (Camera.main == null) return;

        float padding = 2f;
        float bottomPadding = 1f;
        float requiredHeight = boardHeight + padding + bottomPadding;
        float aspectRatio = (float)Screen.width / Screen.height;
        float requiredWidth = boardWidth + 1f;
        float heightFromWidth = (requiredWidth / aspectRatio) / 2f;
        float orthoSize = Mathf.Max(requiredHeight / 2f, heightFromWidth);

        Camera.main.orthographicSize = orthoSize;
        float verticalOffset = (padding - bottomPadding) / 2f;
        Camera.main.transform.position = new Vector3(0, verticalOffset, -10f);
    }

    // ==========================================
    // SPRITE MAPPING
    // ==========================================

    private Sprite GetSpriteForItem(string itemId)
    {
        return itemId switch
        {
            ItemIds.Blue => _blueSprite,
            ItemIds.Red => _redSprite,
            ItemIds.Yellow => _yellowSprite,
            ItemIds.Green => _greenSprite,
            ItemIds.Box => _boxSprite,
            ItemIds.Stone => _stoneSprite,
            ItemIds.Vase => _vaseSprite,
            ItemIds.VerticalRocket => _verticalRocketSprite,
            ItemIds.HorizontalRocket => _horizontalRocketSprite,
            _ => null,
        };
    }

    private Sprite GetVaseSprite(int health)
    {
        if (health <= 1 && _vaseDamagedSprite != null) return _vaseDamagedSprite;
        return _vaseSprite;
    }

    private Sprite GetHintSpriteForColor(string itemId)
    {
        return itemId switch
        {
            ItemIds.Red => _redRocketHint,
            ItemIds.Blue => _blueRocketHint,
            ItemIds.Green => _greenRocketHint,
            ItemIds.Yellow => _yellowRocketHint,
            _ => null,
        };
    }

    // ==========================================
    // SPAWNING & CLEANUP
    // ==========================================

    private CubeView SpawnCube(Coordinate coord, string itemId)
    {
        Vector3 worldPos = GridToWorld(coord);
        GameObject go = Instantiate(_cubePrefab, worldPos, Quaternion.identity, transform);
        CubeView view = go.GetComponent<CubeView>();
        view.Setup(coord, GetSpriteForItem(itemId));
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        _activeCubes[coord] = view;
        return view;
    }

    private Vector3 GridToWorld(Coordinate coord)
    {
        return new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);
    }

    /// <summary>
    /// Safely destroy and remove a CubeView at a coordinate.
    /// No-op if nothing exists there.
    /// </summary>
    private void DestroyCubeAt(Coordinate coord)
    {
        if (_activeCubes.TryGetValue(coord, out var cube))
        {
            if (cube != null) Destroy(cube.gameObject);
            _activeCubes.Remove(coord);
        }
    }

    // ==========================================
    // SPRITE UPDATES
    // ==========================================

    public void UpdateDamagedSprites(Board board, List<Coordinate> damagedCoords)
    {
        foreach (var coord in damagedCoords)
        {
            if (!_activeCubes.TryGetValue(coord, out var cubeView)) continue;
            GridItem item = board.GetItem(coord);
            if (item.Id == ItemIds.Vase)
                cubeView.SetSprite(GetVaseSprite(item.Health));
        }
    }

    // ==========================================
    // ROCKET HINTS
    // ==========================================

    public void UpdateRocketHints(Dictionary<Coordinate, string> newHints)
    {
        // Turn OFF old hints
        foreach (var oldCoord in _currentHintCoords)
        {
            if (!newHints.ContainsKey(oldCoord))
            {
                if (_activeCubes.TryGetValue(oldCoord, out var cube))
                    cube.SetHintVisible(false);
            }
        }

        // Turn ON / update new hints
        foreach (var kvp in newHints)
        {
            if (_activeCubes.TryGetValue(kvp.Key, out var cube))
                cube.SetHintSprite(GetHintSpriteForColor(kvp.Value));
        }

        _currentHintCoords = new HashSet<Coordinate>(newHints.Keys);
    }

    // ==========================================
    // ANIMATION: Blast (< 4 cubes, normal pop)
    // ==========================================

    public async Task AnimateBlast(BlastResult result)
    {
        List<Task> tasks = new List<Task>();

        foreach (var coord in result.BlastedCoordinates)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                tasks.Add(PlayPopAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        foreach (var coord in result.DamagedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
                tasks.Add(PlayDamageShake(cube));
        }

        foreach (var coord in result.DestroyedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                tasks.Add(PlayPopAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        await Task.WhenAll(tasks);
    }

    // ==========================================
    // ANIMATION: Rocket Creation (cubes merge to center)
    // ==========================================

    public async Task AnimateRocketCreation(BlastResult blastResult)
    {
        Coordinate tappedCell = blastResult.RocketSpawnPosition;
        List<Task> tasks = new List<Task>();

        foreach (var coord in blastResult.BlastedCoordinates)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                _activeCubes.Remove(coord);
                if (coord == tappedCell)
                    tasks.Add(PlayShrinkAnimation(cube));
                else
                    tasks.Add(PlayMergeAnimation(cube, tappedCell));
            }
        }

        foreach (var coord in blastResult.DamagedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
                tasks.Add(PlayDamageShake(cube));
        }

        foreach (var coord in blastResult.DestroyedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                tasks.Add(PlayPopAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        await Task.WhenAll(tasks);
    }

    // ==========================================
    // ANIMATION: Rocket Spawn (pop-in after creation)
    // ==========================================

    public void SpawnRocketVisual(RocketCreationData data)
    {
        if (data == null) return;
        DestroyCubeAt(data.SpawnPosition);
        CubeView rocketView = SpawnCube(data.SpawnPosition, data.RocketId);
        rocketView.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUpRoutine(rocketView, 0.2f));
    }

    private System.Collections.IEnumerator ScaleUpRoutine(CubeView cube, float duration)
    {
        float elapsed = 0f;
        Vector3 target = new Vector3(0.95f, 0.95f, 1f);
        while (elapsed < duration)
        {
            if (cube == null) yield break;
            float t = elapsed / duration;
            float ease = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            cube.transform.localScale = target * Mathf.Min(ease * t * 1.2f, 1.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (cube != null) cube.transform.localScale = target;
    }

    // ==========================================
    // ANIMATION: Rocket Explosion (projectile parts)
    // ==========================================

    public async Task AnimateRocketExplosion(RocketExplosionData data)
    {
        // 1. Remove rocket visual at origin
        DestroyCubeAt(data.Origin);

        // 2. Build complete set of cells to destroy visually
        HashSet<Coordinate> allDestroyed = new HashSet<Coordinate>();
        foreach (var c in data.DestroyedCubes) allDestroyed.Add(c);
        foreach (var c in data.DestroyedObstacles) allDestroyed.Add(c);
        foreach (var c in data.TriggeredRockets) allDestroyed.Add(c);

        // 3. Create projectile sprites
        Sprite partASprite, partBSprite;
        if (data.IsHorizontal)
        {
            partASprite = _horizontalPartLeftSprite;
            partBSprite = _horizontalPartRightSprite;
        }
        else
        {
            partASprite = _verticalPartBottomSprite;
            partBSprite = _verticalPartTopSprite;
        }

        GameObject partAObj = CreateProjectile(data.Origin, partASprite);
        GameObject partBObj = CreateProjectile(data.Origin, partBSprite);

        // 4. Animate both paths in parallel
        await Task.WhenAll(
            AnimateProjectilePath(partAObj, data.PathA, allDestroyed),
            AnimateProjectilePath(partBObj, data.PathB, allDestroyed)
        );

        // 5. Destroy projectiles
        if (partAObj != null) Destroy(partAObj);
        if (partBObj != null) Destroy(partBObj);

        // 6. Cleanup pass: destroy off-path cells (3×3 corners in combos)
        foreach (var coord in allDestroyed)
            DestroyCubeAt(coord);

        // 7. Shake damaged obstacles
        List<Task> shakeTasks = new List<Task>();
        foreach (var coord in data.DamagedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
                shakeTasks.Add(PlayDamageShake(cube));
        }
        if (shakeTasks.Count > 0)
            await Task.WhenAll(shakeTasks);
    }

    private GameObject CreateProjectile(Coordinate origin, Sprite sprite)
    {
        Vector3 pos = GridToWorld(origin);
        GameObject go = new GameObject("RocketProjectile");
        go.transform.position = pos;
        go.transform.SetParent(transform);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        return go;
    }

    private async Task AnimateProjectilePath(
        GameObject projectile, List<Coordinate> path, HashSet<Coordinate> destroyedCoords)
    {
        if (projectile == null || path.Count == 0) return;

        float moveTime = 0.04f; // Per cell speed

        for (int i = 0; i < path.Count; i++)
        {
            if (projectile == null) return;

            Coordinate cell = path[i];
            Vector3 targetPos = GridToWorld(cell);
            Vector3 startPos = projectile.transform.position;
            float elapsed = 0f;

            while (elapsed < moveTime)
            {
                if (projectile == null) return;
                projectile.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / moveTime);
                elapsed += Time.deltaTime;
                await Task.Yield();
            }

            if (projectile != null)
                projectile.transform.position = targetPos;

            // Destroy cell visual as projectile passes
            if (destroyedCoords.Contains(cell))
            {
                if (_activeCubes.TryGetValue(cell, out var cellCube))
                {
                    if (cellCube != null) Destroy(cellCube.gameObject);
                    _activeCubes.Remove(cell);
                }
            }
        }
    }

    // ==========================================
    // ANIMATION: Gravity
    // ==========================================

    public async Task AnimateGravity(List<ItemMovement> movements)
    {
        List<Task> tasks = new List<Task>();

        foreach (var move in movements)
        {
            if (_activeCubes.TryGetValue(move.StartPos, out CubeView cube))
            {
                _activeCubes.Remove(move.StartPos);
                DestroyCubeAt(move.EndPos); // Defensive: clear stale
                _activeCubes[move.EndPos] = cube;
                tasks.Add(SlideCubeVisually(cube, move.EndPos, 0.3f));
            }
        }

        await Task.WhenAll(tasks);
    }

    // ==========================================
    // ANIMATION: Refill
    // ==========================================

    public async Task AnimateRefill(List<ItemMovement> newItems)
    {
        List<Task> tasks = new List<Task>();

        foreach (var move in newItems)
        {
            DestroyCubeAt(move.EndPos); // Defensive: clear stale

            Vector3 spawnWorldPos = GridToWorld(move.StartPos);
            GameObject go = Instantiate(_cubePrefab, spawnWorldPos, Quaternion.identity, transform);
            CubeView view = go.GetComponent<CubeView>();
            view.Setup(move.StartPos, GetSpriteForItem(move.ItemId));
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            _activeCubes[move.EndPos] = view;
            tasks.Add(SlideCubeVisually(view, move.EndPos, 0.4f));
        }

        await Task.WhenAll(tasks);
    }

    // ==========================================
    // CORE ANIMATION HELPERS
    // ==========================================

    private async Task SlideCubeVisually(CubeView cube, Coordinate targetCoord, float duration)
    {
        Vector3 startPos = cube.transform.position;
        Vector3 endPos = GridToWorld(targetCoord);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null) return;
            float t = elapsed / duration;
            cube.transform.position = Vector3.Lerp(startPos, endPos, t * t);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null)
        {
            cube.transform.position = endPos;
            cube.UpdateCoordinate(targetCoord);
        }
    }

    private async Task PlayMergeAnimation(CubeView cube, Coordinate targetCoord)
    {
        if (cube == null) return;
        Vector3 startPos = cube.transform.position;
        Vector3 endPos = GridToWorld(targetCoord);
        Vector3 startScale = cube.transform.localScale;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null) return;
            float t = elapsed / duration;
            cube.transform.position = Vector3.Lerp(startPos, endPos, t * t);
            cube.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null) Destroy(cube.gameObject);
    }

    private async Task PlayShrinkAnimation(CubeView cube)
    {
        if (cube == null) return;
        Vector3 startScale = cube.transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null) return;
            cube.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null) Destroy(cube.gameObject);
    }

    private async Task PlayPopAnimation(CubeView cube)
    {
        if (cube == null) return;
        cube.transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(80);
        if (cube != null) Destroy(cube.gameObject);
    }

    private async Task PlayDamageShake(CubeView cube)
    {
        if (cube == null) return;
        Vector3 original = cube.transform.position;
        float mag = 0.08f;

        for (int i = 0; i < 4; i++)
        {
            if (cube == null) return;
            cube.transform.position = original + Vector3.right * mag;
            await Task.Delay(30);
            if (cube == null) return;
            cube.transform.position = original - Vector3.right * mag;
            await Task.Delay(30);
        }

        if (cube != null) cube.transform.position = original;
    }
}
