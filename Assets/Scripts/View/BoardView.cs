using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private GameObject _cubePrefab;

    [SerializeField]
    private GameOrchestrator _orchestrator;

    [Header("Cube Sprites")]
    [SerializeField]
    private Sprite _blueSprite;

    [SerializeField]
    private Sprite _redSprite;

    [SerializeField]
    private Sprite _yellowSprite;

    [SerializeField]
    private Sprite _greenSprite;

    private Dictionary<Coordinate, CubeView> _activeCubes = new();

    // We will store the offsets here so we can reuse them later
    private float _offsetX;
    private float _offsetY;

    public void Initialize(Board board, GameOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;

        // Calculate the exact center of the board
        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                SpawnCube(new Coordinate(x, y), board.GetGem(x, y));
            }
        }
    }

    private void SpawnCube(Coordinate coord, Gem type)
    {
        // FIX: Subtract the offset so the board builds outward from the center
        Vector3 spawnPosition = new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);

        GameObject go = Instantiate(_cubePrefab, spawnPosition, Quaternion.identity, transform);
        CubeView view = go.GetComponent<CubeView>();

        Sprite targetSprite = GetSpriteForGem(type);
        view.Setup(coord, targetSprite, _orchestrator);

        // Optional visual tweak: scale down slightly to create a tiny gap between cubes
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        _activeCubes[coord] = view;
    }

    private Sprite GetSpriteForGem(Gem type) =>
        type switch
        {
            Gem.BLUE => _blueSprite,
            Gem.RED => _redSprite,
            Gem.YELLOW => _yellowSprite,
            Gem.GREEN => _greenSprite,
            _ => null,
        };

    public async Task AnimateBlast(List<Coordinate> coords)
    {
        List<Task> animTasks = new();
        foreach (var coord in coords)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                animTasks.Add(cube.PlayPopAnimation());
                _activeCubes.Remove(coord);
            }
        }
        await Task.WhenAll(animTasks);
    }
}
