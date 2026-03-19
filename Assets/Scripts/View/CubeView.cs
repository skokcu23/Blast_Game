using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
/// Hint system: swaps between default sprite and rocket-state sprite
/// (same approach as Toon Blast — full cube visual changes, no overlay).
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Sprite _defaultSprite;

    public Coordinate GridCoordinate { get; private set; }

    public void Setup(Coordinate coord, Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
        _defaultSprite = sprite;
        UpdateCoordinate(coord);
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
    /// Revert to the default (non-hint) sprite.
    /// </summary>
    public void SetHintVisible(bool visible)
    {
        if (!visible && _spriteRenderer != null && _defaultSprite != null)
        {
            _spriteRenderer.sprite = _defaultSprite;

            // Quick punch so the swap doesn't feel instant
            DOTween.Kill(transform, "hint");
            transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 1, 0)
                .SetId("hint");
        }
    }

    public void SetHintSprite(Sprite rocketStateSprite)
    {
        if (_spriteRenderer == null) return;

        if (rocketStateSprite != null)
        {
            _spriteRenderer.sprite = rocketStateSprite;

            // Pop in: scale up slightly then settle
            DOTween.Kill(transform, "hint");
            transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 1, 0)
                .SetId("hint");
        }
        else if (_defaultSprite != null)
        {
            _spriteRenderer.sprite = _defaultSprite;
        }
    }
}
