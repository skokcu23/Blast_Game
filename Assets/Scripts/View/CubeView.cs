using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
/// Simplified: stores data, renders sprite. No animation logic.
///
/// Knows its ItemId so GridStateManager can compare against Board state.
/// Knows its default sprite so hint system can swap and revert.
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public string ItemId { get; private set; }
    public Coordinate GridCoordinate { get; private set; }

    private Sprite _defaultSprite;

    /// <summary>
    /// Configure this CubeView for a specific cell.
    /// Called by GridStateManager when placing a cell.
    /// </summary>
    public void Configure(Coordinate coord, Sprite sprite, string itemId)
    {
        ItemId = itemId;
        GridCoordinate = coord;
        _defaultSprite = sprite;
        _spriteRenderer.sprite = sprite;
        name = $"Cell_{coord.x}_{coord.y}";
    }

    /// <summary>
    /// Reset all state. Called by CubePool.Return() before deactivating.
    /// </summary>
    public void Reset()
    {
        DOTween.Kill(transform);
        DOTween.Kill(transform, "hint");
        ItemId = null;
        GridCoordinate = default;
        _defaultSprite = null;
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = null;
        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Update which coordinate this view represents.
    /// Called by GridStateManager.MoveCell during gravity.
    /// </summary>
    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
    }

    /// <summary>
    /// Change the displayed sprite (e.g., vase cracking).
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprite;
    }

    /// <summary>
    /// Update the default sprite (what revert restores to).
    /// Used when the item type hasn't changed but the base visual has.
    /// </summary>
    public void SetDefaultSprite(Sprite sprite)
    {
        _defaultSprite = sprite;
    }

    /// <summary>
    /// Revert to the default (non-hint) sprite with punch animation.
    /// </summary>
    public void RevertToDefault()
    {
        if (_spriteRenderer != null && _defaultSprite != null)
        {
            _spriteRenderer.sprite = _defaultSprite;

            DOTween.Kill(transform, "hint");
            transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 1, 0)
                .SetId("hint");
        }
    }

    /// <summary>
    /// Swap to rocket-state sprite (Toon Blast style hint).
    /// </summary>
    public void SetHintSprite(Sprite rocketStateSprite)
    {
        if (_spriteRenderer == null) return;

        if (rocketStateSprite != null)
        {
            _spriteRenderer.sprite = rocketStateSprite;

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
