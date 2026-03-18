using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The View layer: visually reflects the Board's logical state.
///
/// 3A additions:
///   - AnimateRocketCreation(): cubes merge to center, then rocket spawns
///   - SpawnRocketVisual(): places a rocket CubeView on the board
///   - UpdateRocketHints(): shows/hides hint overlays on eligible groups
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    private GameObject _cubePrefab;

    [Header("Cube Sprites")]
    [SerializeField]
    private Sprite _blueSprite;

    [SerializeField]
    private Sprite _redSprite;

    [SerializeField]
    private Sprite _yellowSprite;

    [SerializeField]
    private Sprite _greenSprite;

    [Header("Rocket Hint Sprites")]
    [SerializeField]
    private Sprite _redRocketHint;

    [SerializeField]
    private Sprite _blueRocketHint;

    [SerializeField]
    private Sprite _greenRocketHint;

    [SerializeField]
    private Sprite _yellowRocketHint;

    [Header("Obstacle Sprites")]
    [SerializeField]
    private Sprite _boxSprite;

    [SerializeField]
    private Sprite _stoneSprite;

    [SerializeField]
    private Sprite _vaseSprite;

    [SerializeField]
    private Sprite _vaseDamagedSprite;

    [Header("Rocket Sprites")]
    [SerializeField]
    private Sprite _verticalRocketSprite;

    [SerializeField]
    private Sprite _horizontalRocketSprite;

    private Dictionary<Coordinate, CubeView> _activeCubes = new Dictionary<Coordinate, CubeView>();
    private float _offsetX;
    private float _offsetY;

    // Track which cubes currently show hints (for efficient toggling)
    private HashSet<Coordinate> _currentHintCoords = new HashSet<Coordinate>();

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(Board board)
    {
        foreach (var kvp in _activeCubes)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _activeCubes.Clear();
        _currentHintCoords.Clear();

        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Coordinate coord = new Coordinate(x, y);
                GridItem item = board.GetItem(coord);

                if (item.IsEmpty)
                    continue;

                SpawnCube(coord, item.Id);
            }
        }

        FitCameraToBoard(board.Width, board.Height);
    }

    /// <summary>
    /// Adjusts the main camera's orthographic size so the entire board
    /// fits on screen with padding, regardless of grid dimensions.
    /// Works for any aspect ratio but optimized for 9:16 portrait.
    /// </summary>
    private void FitCameraToBoard(int boardWidth, int boardHeight)
    {
        if (Camera.main == null) return;

        float padding = 2f; // Extra space for UI (move counter, goals) at top
        float bottomPadding = 1f;

        // How much vertical space the board needs
        float requiredHeight = boardHeight + padding + bottomPadding;

        // How much horizontal space the board needs
        float aspectRatio = (float)Screen.width / Screen.height;
        float requiredWidth = boardWidth + 1f; // Small horizontal padding
        float heightFromWidth = (requiredWidth / aspectRatio) / 2f;

        // Use whichever is larger (so nothing gets clipped)
        float orthoSize = Mathf.Max(requiredHeight / 2f, heightFromWidth);

        Camera.main.orthographicSize = orthoSize;

        // Shift camera slightly up so there's room for UI at top
        // and board sits comfortably in the lower portion
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
        if (health <= 1 && _vaseDamagedSprite != null)
            return _vaseDamagedSprite;
        return _vaseSprite;
    }

    // ==========================================
    // SPAWNING
    // ==========================================

    private CubeView SpawnCube(Coordinate coord, string itemId)
    {
        Vector3 worldPos = GridToWorld(coord);
        GameObject go = Instantiate(_cubePrefab, worldPos, Quaternion.identity, transform);

        CubeView view = go.GetComponent<CubeView>();
        Sprite sprite = GetSpriteForItem(itemId);
        view.Setup(coord, sprite);
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        _activeCubes[coord] = view;
        return view;
    }

    private Vector3 GridToWorld(Coordinate coord)
    {
        return new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);
    }

    // ==========================================
    // SPRITE UPDATES
    // ==========================================

    public void UpdateDamagedSprites(Board board, List<Coordinate> damagedCoords)
    {
        foreach (var coord in damagedCoords)
        {
            if (!_activeCubes.TryGetValue(coord, out var cubeView))
                continue;

            GridItem item = board.GetItem(coord);

            if (item.Id == ItemIds.Vase)
                cubeView.SetSprite(GetVaseSprite(item.Health));
        }
    }

    // ==========================================
    // ROCKET HINTS
    // ==========================================

    /// <summary>
    /// Update which cubes show the rocket hint overlay.
    /// Efficiently diffs against the previous state to minimize changes.
    /// </summary>
    public void UpdateRocketHints(Dictionary<Coordinate, string> newHints)
    {
        // Turn OFF hints no longer eligible
        foreach (var oldCoord in _currentHintCoords)
        {
            if (!newHints.ContainsKey(oldCoord))
            {
                if (_activeCubes.TryGetValue(oldCoord, out var cube))
                    cube.SetHintVisible(false);
            }
        }

        // Turn ON / update hints
        foreach (var kvp in newHints)
        {
            if (_activeCubes.TryGetValue(kvp.Key, out var cube))
            {
                Sprite hintSprite = GetHintSpriteForColor(kvp.Value);
                cube.SetHintSprite(hintSprite);
            }
        }

        _currentHintCoords = new HashSet<Coordinate>(newHints.Keys);
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
    // ANIMATION: Normal Blast (< 4 cubes)
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
                tasks.Add(PlayObstacleDestroyAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        await Task.WhenAll(tasks);
    }

    // ==========================================
    // ANIMATION: Rocket Creation (≥ 4 cubes merge)
    // ==========================================

    /// <summary>
    /// Animate cubes merging toward the tapped cell, then play obstacle animations.
    /// The cubes slide inward and disappear. The rocket visual is spawned separately
    /// after this animation completes (via SpawnRocketVisual).
    ///
    /// Case Study: "cubes need to animate to the clicked cell and create a Rocket."
    /// </summary>
    public async Task AnimateRocketCreation(BlastResult blastResult, Coordinate tappedCell)
    {
        List<Task> mergeTasks = new List<Task>();

        // 1. Slide all matched cubes toward the tapped cell
        foreach (var coord in blastResult.BlastedCoordinates)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                _activeCubes.Remove(coord);

                if (coord == tappedCell)
                {
                    // The tapped cell just scales down and disappears
                    mergeTasks.Add(PlayShrinkAnimation(cube));
                }
                else
                {
                    // Other cubes slide toward the tapped cell then disappear
                    mergeTasks.Add(PlayMergeAnimation(cube, tappedCell));
                }
            }
        }

        // 2. Obstacle damage animations happen in parallel with merge
        foreach (var coord in blastResult.DamagedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
                mergeTasks.Add(PlayDamageShake(cube));
        }

        foreach (var coord in blastResult.DestroyedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                mergeTasks.Add(PlayObstacleDestroyAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        await Task.WhenAll(mergeTasks);
    }

    /// <summary>
    /// Spawn the rocket visual at its position on the board.
    /// Called by the Orchestrator after CreateRocket() places it in logic.
    /// </summary>
    public void SpawnRocketVisual(RocketCreationData data)
    {
        if (data == null)
            return;

        // Remove any leftover visual at this position (shouldn't happen, but safety)
        if (_activeCubes.TryGetValue(data.SpawnPosition, out var existing))
        {
            Destroy(existing.gameObject);
            _activeCubes.Remove(data.SpawnPosition);
        }

        CubeView rocketView = SpawnCube(data.SpawnPosition, data.RocketId);

        // Quick scale-up "pop in" for visual impact
        rocketView.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUpRoutine(rocketView, 0.2f));
    }

    private System.Collections.IEnumerator ScaleUpRoutine(CubeView cube, float duration)
    {
        float elapsed = 0f;
        Vector3 target = new Vector3(0.95f, 0.95f, 1f);

        while (elapsed < duration)
        {
            if (cube == null)
                yield break;

            float t = elapsed / duration;
            // Overshoot ease: goes slightly past 1.0 then settles
            float ease = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            cube.transform.localScale = target * Mathf.Min(ease * t * 1.2f, 1.1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cube != null)
            cube.transform.localScale = target;
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
            if (cube == null)
                return;
            float t = elapsed / duration;
            t = t * t; // Ease-in
            cube.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null)
        {
            cube.transform.position = endPos;
            cube.UpdateCoordinate(targetCoord);
        }
    }

    /// <summary>
    /// Cube slides toward a target coordinate, shrinks, and gets destroyed.
    /// Used for rocket creation merge animation.
    /// </summary>
    private async Task PlayMergeAnimation(CubeView cube, Coordinate targetCoord)
    {
        if (cube == null)
            return;

        Vector3 startPos = cube.transform.position;
        Vector3 endPos = GridToWorld(targetCoord);
        Vector3 startScale = cube.transform.localScale;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null)
                return;

            float t = elapsed / duration;
            cube.transform.position = Vector3.Lerp(startPos, endPos, t * t);
            cube.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null)
            Destroy(cube.gameObject);
    }

    private async Task PlayShrinkAnimation(CubeView cube)
    {
        if (cube == null)
            return;

        Vector3 startScale = cube.transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null)
                return;
            float t = elapsed / duration;
            cube.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null)
            Destroy(cube.gameObject);
    }

    private async Task PlayPopAnimation(CubeView cube)
    {
        if (cube == null)
            return;
        cube.transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(80);
        if (cube != null)
            Destroy(cube.gameObject);
    }

    private async Task PlayObstacleDestroyAnimation(CubeView cube)
    {
        if (cube == null)
            return;
        cube.transform.localScale = Vector3.one * 1.1f;
        await Task.Delay(60);
        if (cube == null)
            return;
        cube.transform.localScale = Vector3.one * 0.5f;
        await Task.Delay(60);
        if (cube != null)
            Destroy(cube.gameObject);
    }

    private async Task PlayDamageShake(CubeView cube)
    {
        if (cube == null)
            return;

        Vector3 original = cube.transform.position;
        float mag = 0.08f;

        for (int i = 0; i < 4; i++)
        {
            if (cube == null)
                return;
            cube.transform.position = original + Vector3.right * mag;
            await Task.Delay(30);
            if (cube == null)
                return;
            cube.transform.position = original - Vector3.right * mag;
            await Task.Delay(30);
        }

        if (cube != null)
            cube.transform.position = original;
    }
}
