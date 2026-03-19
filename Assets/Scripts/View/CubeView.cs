using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
///
/// Hint system: each CubeView owns its own hint state (_isHinted).
/// ApplyHint/RemoveHint are idempotent — calling them when already
/// in the target state is a no-op. No external tracking needed.
///
/// Sprite swap approach (Toon Blast style):
///   Normal: blue sprite
///   Hinted: blue_rocket sprite (full replacement, not overlay)
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public string ItemId { get; private set; }
    public Coordinate GridCoordinate { get; private set; }

    private Sprite _defaultSprite;
    private bool _isHinted;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    /// <summary>
    /// Configure this CubeView for a specific cell.
    /// Called by GridStateManager.PlaceCell.
    /// </summary>
    public void Configure(Coordinate coord, Sprite sprite, string itemId)
    {
        ItemId = itemId;
        GridCoordinate = coord;
        _defaultSprite = sprite;
        _isHinted = false;

        if (_spriteRenderer != null)
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
        _isHinted = false;

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = null;

        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        transform.localPosition = Vector3.zero;
    }

    // ==========================================
    // COORDINATE
    // ==========================================

    /// <summary>
    /// Update which coordinate this view represents.
    /// Called by GridStateManager.MoveCell during gravity.
    /// Note: does NOT affect hint state — the CubeView keeps
    /// its _isHinted flag through movement.
    /// </summary>
    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
    }

    // ==========================================
    // SPRITE
    // ==========================================

    /// <summary>
    /// Change the displayed sprite (e.g., vase cracking).
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprite;
    }

    /// <summary>
    /// Update the default sprite (what RemoveHint restores to).
    /// Used when the base visual changes (vase crack) but item type stays same.
    /// </summary>
    public void SetDefaultSprite(Sprite sprite)
    {
        _defaultSprite = sprite;
    }

    // ==========================================
    // HINT SYSTEM (Stateless, Idempotent)
    // ==========================================

    /// <summary>
    /// Apply hint visual. Swaps sprite to rocket-state version.
    /// No-op if already hinted — prevents duplicate animations.
    /// </summary>
    public void ApplyHint(Sprite hintSprite)
    {
        if (_isHinted) return;
        if (_spriteRenderer == null || hintSprite == null) return;

        _isHinted = true;
        _spriteRenderer.sprite = hintSprite;

        DOTween.Kill(transform, "hint");
        transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 1, 0)
            .SetId("hint");
    }

    /// <summary>
    /// Remove hint visual. Reverts sprite to default.
    /// No-op if not hinted — prevents duplicate animations.
    /// </summary>
    public void RemoveHint()
    {
        if (!_isHinted) return;
        if (_spriteRenderer == null || _defaultSprite == null) return;

        _isHinted = false;
        _spriteRenderer.sprite = _defaultSprite;

        DOTween.Kill(transform, "hint");
        transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 1, 0)
            .SetId("hint");
    }
}