using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
///
/// Owns two derived visual systems:
///   Hints:  ApplyHint/RemoveHint — idempotent sprite swap based on group membership
///   Damage: ApplyDamageVisual — instant sprite swap when an obstacle takes damage
///
/// Sorting order is derived from Y coordinate — lower rows render behind higher rows,
/// creating the 3D stacked cube illusion with overlapping bevel edges.
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public string ItemId { get; private set; }
    public Coordinate GridCoordinate { get; private set; }

    private Sprite _defaultSprite;
    private bool _isHinted;
    private int _displayedHealth;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    /// <summary>Configure this CubeView for a specific cell. Sets sprite, coordinate, and sorting order.</summary>
    public void Configure(Coordinate coord, Sprite sprite, string itemId)
    {
        ItemId = itemId;
        GridCoordinate = coord;
        _defaultSprite = sprite;
        _isHinted = false;
        _displayedHealth = -1;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingOrder = coord.y;
        }

        name = $"Cell_{coord.x}_{coord.y}";
    }

    /// <summary>Configure with explicit health tracking (for obstacles like Vase).</summary>
    public void ConfigureWithHealth(Coordinate coord, Sprite sprite, string itemId, int health)
    {
        Configure(coord, sprite, itemId);
        _displayedHealth = health;
    }

    /// <summary>Reset all state. Called by CubePool.Return() before deactivating.</summary>
    public void Reset()
    {
        DOTween.Kill(transform);
        DOTween.Kill(transform, "hint");

        ItemId = null;
        GridCoordinate = default;
        _defaultSprite = null;
        _isHinted = false;
        _displayedHealth = -1;

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = null;

        transform.localScale = BoardView.CellScale;
        transform.localPosition = Vector3.zero;
    }

    // ==========================================
    // COORDINATE
    // ==========================================

    /// <summary>Update coordinate after gravity. Also updates sorting order.</summary>
    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
        SetSortingOrder(newCoord.y);
    }

    // ==========================================
    // SPRITE
    // ==========================================

    public void SetSprite(Sprite sprite)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = sprite;
    }

    public void SetDefaultSprite(Sprite sprite)
    {
        _defaultSprite = sprite;
    }

    // ==========================================
    // HINTS (Stateless, Idempotent)
    // ==========================================

    /// <summary>Apply rocket hint visual. No-op if already hinted.</summary>
    public void ApplyHint(Sprite hintSprite)
    {
        if (_isHinted) return;
        if (_spriteRenderer == null || hintSprite == null) return;

        _isHinted = true;
        _spriteRenderer.sprite = hintSprite;

        DOTween.Kill(transform, "hint");
        transform.DOPunchScale(Vector3.one * 0.12f, 0.05f, 1, 0)
            .SetId("hint");
    }

    /// <summary>Remove rocket hint visual. No-op if not hinted.</summary>
    public void RemoveHint()
    {
        if (!_isHinted) return;
        if (_spriteRenderer == null || _defaultSprite == null) return;

        _isHinted = false;
        _spriteRenderer.sprite = _defaultSprite;

        DOTween.Kill(transform, "hint");
        transform.DOPunchScale(Vector3.one * 0.08f, 0.05f, 1, 0)
            .SetId("hint");
    }

    // ==========================================
    // DAMAGE VISUAL (Instant)
    // ==========================================

    /// <summary>
    /// Apply damage visual instantly (e.g., vase cracking).
    /// Called by AnimationController using pre-gravity coordinates.
    /// </summary>
    public void ApplyDamageVisual(Sprite damagedSprite)
    {
        if (damagedSprite == null) return;
        SetSprite(damagedSprite);
        SetDefaultSprite(damagedSprite);
        if (_displayedHealth > 0) _displayedHealth--;
    }

    // ==========================================
    // SORTING
    // ==========================================

    public void SetSortingOrder(int order)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sortingOrder = order;
    }
}
