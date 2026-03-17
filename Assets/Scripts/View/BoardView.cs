using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("Configuration")]
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

    private Dictionary<Coordinate, CubeView> _activeCubes = new();
    private float _offsetX;
    private float _offsetY;

    public void Initialize(Board board)
    {
        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                SpawnCube(new Coordinate(x, y), board.GetItem(x, y).GemType);
            }
        }
    }

    private void SpawnCube(Coordinate coord, Gem type)
    {
        Vector3 spawnPosition = new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);

        GameObject go = Instantiate(_cubePrefab, spawnPosition, Quaternion.identity, transform);
        CubeView view = go.GetComponent<CubeView>();

        Sprite targetSprite = GetSpriteForGem(type);
        view.Setup(coord, targetSprite);

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

    // Stage Manager directly moves the puppet, respecting the visual offsets!
    private async Task SlideCubeVisually(
        CubeView cube,
        Coordinate targetCoord,
        float duration = 0.3f
    )
    {
        Vector3 startPosition = cube.transform.position;
        // FIX: Re-applied the offset so the visual alignment doesn't break
        Vector3 targetPosition = new Vector3(targetCoord.x - _offsetX, targetCoord.y - _offsetY, 0);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cube == null)
                return;

            cube.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                elapsed / duration
            );
            elapsed += Time.deltaTime;
            await Task.Yield();
        }

        if (cube != null)
        {
            cube.transform.position = targetPosition;
            cube.UpdateCoordinate(targetCoord);
        }
    }

    public async Task AnimateBlast(List<Coordinate> coords)
    {
        List<Task> animTasks = new();
        foreach (var coord in coords)
        {
            if (_activeCubes.TryGetValue(coord, out var cube))
            {
                // Pass the specific cube to the animation method
                animTasks.Add(PlayPopAnimation(cube));
                _activeCubes.Remove(coord);
            }
        }
        await Task.WhenAll(animTasks);
    }

    // FIX: Takes a specific CubeView and destroys its gameObject, not the Board's!
    private async Task PlayPopAnimation(CubeView cube)
    {
        if (cube == null)
            return;

        cube.transform.localScale = Vector3.one * 1.2f;
        await Task.Delay(100);

        if (cube != null)
            Destroy(cube.gameObject);
    }

    public async Task AnimateGravity(List<GemMovement> movements)
    {
        List<Task> moveTasks = new List<Task>();

        foreach (var move in movements)
        {
            if (_activeCubes.TryGetValue(move.StartPos, out CubeView cube))
            {
                _activeCubes.Remove(move.StartPos);
                _activeCubes[move.EndPos] = cube;
                moveTasks.Add(SlideCubeVisually(cube, move.EndPos));
            }
        }

        await Task.WhenAll(moveTasks);
    }

    public async Task AnimateRefill(List<GemMovement> newGems)
    {
        List<Task> spawnTasks = new List<Task>();

        foreach (var move in newGems)
        {
            // FIX: Spawn above the board, but also respecting the X offset
            Vector3 spawnWorldPos = new Vector3(
                move.StartPos.x - _offsetX,
                move.StartPos.y - _offsetY,
                0
            );

            GameObject newCubeObj = Instantiate(
                _cubePrefab,
                spawnWorldPos,
                Quaternion.identity,
                transform
            );
            CubeView newCubeView = newCubeObj.GetComponent<CubeView>();

            newCubeView.Setup(move.StartPos, GetSpriteForGem(move.GemType));
            _activeCubes[move.EndPos] = newCubeView;

            spawnTasks.Add(SlideCubeVisually(newCubeView, move.EndPos, 0.4f));
        }

        await Task.WhenAll(spawnTasks);
    }
}
