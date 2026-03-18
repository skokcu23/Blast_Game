using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The View layer: visually reflects the Board's logical state.
/// Knows nothing about game rules — only receives instructions from the Orchestrator.
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

    [Header("Obstacle Sprites")]
    [SerializeField] private Sprite _boxSprite;
    [SerializeField] private Sprite _stoneSprite;
    [SerializeField] private Sprite _vaseSprite;
    [SerializeField] private Sprite _vaseDamagedSprite;

    [Header("Rocket Sprites")]
    [SerializeField] private Sprite _verticalRocketSprite;
    [SerializeField] private Sprite _horizontalRocketSprite;

    private Dictionary<Coordinate, CubeView> _activeCubes = new Dictionary<Coordinate, CubeView>();
    private float _offsetX;
    private float _offsetY;

    // --- Initialization ---

    public void Initialize(Board board)
    {
        // Clear previous level
        foreach (var kvp in _activeCubes)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _activeCubes.Clear();

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
    }

    // --- Sprite Mapping ---

    private Sprite GetSpriteForItem(string itemId)
    {
        return itemId switch
        {
            ItemIds.Blue   => _blueSprite,
            ItemIds.Red    => _redSprite,
            ItemIds.Yellow => _yellowSprite,
            ItemIds.Green  => _greenSprite,
            ItemIds.Box    => _boxSprite,
            ItemIds.Stone  => _stoneSprite,
            ItemIds.Vase   => _vaseSprite,
            ItemIds.VerticalRocket   => _verticalRocketSprite,
            ItemIds.HorizontalRocket => _horizontalRocketSprite,
            _ => null
        };
    }

    /// <summary>
    /// Get the correct sprite for a vase based on remaining health.
    /// Full health → normal sprite, damaged → cracked sprite.
    /// </summary>
    private Sprite GetVaseSprite(int health)
    {
        if (health <= 1 && _vaseDamagedSprite != null)
            return _vaseDamagedSprite;
        return _vaseSprite;
    }

    // --- Spawning ---

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

    // --- Coordinate Conversion ---

    private Vector3 GridToWorld(Coordinate coord)
    {
        return new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);
    }

    // --- Sprite Updates (called by Orchestrator after damage) ---

    /// <summary>
    /// After a blast, update sprites for obstacles that took damage but survived.
    /// Queries the Board for current health to determine correct visual.
    /// </summary>
    public void UpdateDamagedSprites(Board board, List<Coordinate> damagedCoords)
    {
        foreach (var coord in damagedCoords)
        {
            if (!_activeCubes.TryGetValue(coord, out var cubeView))
                continue;

            GridItem item = board.GetItem(coord);

            if (item.Id == ItemIds.Vase)
            {
                cubeView.SetSprite(GetVaseSprite(item.Health));
            }
            // Future: other obstacle visual states can be added here
        }
    }

    // --- Animation: Blast ---

    public async Task AnimateBlast(BlastResult result)
    {
        List<Task> tasks = new List<Task>();

        // 1. Pop matched cubes
        foreach (var coord in result.BlastedCoordinates)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                tasks.Add(PlayPopAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }

        // 2. Shake damaged obstacles
        foreach (var coord in result.DamagedObstacles)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                tasks.Add(PlayDamageShake(cube));
            }
        }

        // 3. Pop destroyed obstacles
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

    // --- Animation: Gravity ---

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

    // --- Animation: Refill ---

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

    // --- Core Animation Helpers ---

    private async Task SlideCubeVisually(CubeView cube, Coordinate targetCoord, float duration)
    {
        Vector3 startPos = cube.transform.position;
        Vector3 endPos = GridToWorld(targetCoord);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null) return;
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

    private async Task PlayPopAnimation(CubeView cube)
    {
        if (cube == null) return;
        cube.transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(80);
        if (cube != null) Destroy(cube.gameObject);
    }

    private async Task PlayObstacleDestroyAnimation(CubeView cube)
    {
        if (cube == null) return;

        // Slightly different from cube pop: flash white then shrink
        cube.transform.localScale = Vector3.one * 1.1f;
        await Task.Delay(60);

        if (cube == null) return;
        cube.transform.localScale = Vector3.one * 0.5f;
        await Task.Delay(60);

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
