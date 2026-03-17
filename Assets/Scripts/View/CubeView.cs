using UnityEngine;

/// <summary>
/// Visual representation of a single grid cell.
/// A simple "puppet" — it holds a sprite and its grid coordinate.
/// All movement and lifecycle is controlled by BoardView.
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Coordinate GridCoordinate { get; private set; }

    public void Setup(Coordinate coord, Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
        UpdateCoordinate(coord);
    }

    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
    }

    /// <summary>
    /// Swap sprite at runtime (e.g., vase cracking, rocket hint overlay).
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprite;
    }
}
