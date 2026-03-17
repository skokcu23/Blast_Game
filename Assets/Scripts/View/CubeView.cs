using UnityEngine;

public class CubeView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    public Coordinate GridCoordinate { get; private set; }

    public void Setup(Coordinate coord, Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
        UpdateCoordinate(coord);
    }

    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cube_{newCoord.x}_{newCoord.y}";
    }
}
