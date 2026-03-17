using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [SerializeField]
    private GameObject _cubePrefab;
    private Dictionary<Coordinate, CubeView> _activeCubes = new();

    public void Initialize(Board board)
    {
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
        GameObject go = Instantiate(
            _cubePrefab,
            new Vector3(coord.x, coord.y, 0),
            Quaternion.identity,
            transform
        );
        CubeView view = go.GetComponent<CubeView>();
        view.Setup(coord, type);
        _activeCubes[coord] = view;
    }

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
