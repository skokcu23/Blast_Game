using UnityEngine;

/// <summary>
/// Visual representation of a single grid cell.
/// A "puppet" — it holds a sprite, its grid coordinate, and an optional hint overlay.
/// All movement and lifecycle is controlled by BoardView.
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Hint Overlay (optional)")]
    [SerializeField] private SpriteRenderer _hintOverlay;

    public Coordinate GridCoordinate { get; private set; }

    public void Setup(Coordinate coord, Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
        UpdateCoordinate(coord);
        SetHintVisible(false);
    }

    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
    }

    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprite;
    }

    /// <summary>
    /// Show or hide the rocket hint overlay on this cell.
    /// </summary>
    public void SetHintVisible(bool visible)
    {
        if (_hintOverlay != null)
            _hintOverlay.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Show the hint overlay with a specific sprite (color-matched rocket icon).
    /// </summary>
    public void SetHintSprite(Sprite sprite)
    {
        if (_hintOverlay == null) return;

        if (sprite != null)
        {
            _hintOverlay.sprite = sprite;
            _hintOverlay.gameObject.SetActive(true);
        }
        else
        {
            _hintOverlay.gameObject.SetActive(false);
        }
    }
}
