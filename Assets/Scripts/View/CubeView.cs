using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
///
/// Two derived visual systems:
///   Hints:  ApplyHint/RemoveHint — sprite swap based on group membership
///   Damage: ApplyDamageVisual — instant sprite swap when obstacle takes damage
///
/// Both are idempotent. No animation timing conflicts.
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
            _spriteRenderer.sortingOrder = coord.y;  // add this
        }


        name = $"Cell_{coord.x}_{coord.y}";
    }

    public void ConfigureWithHealth(Coordinate coord, Sprite sprite, string itemId, int health)
    {
        Configure(coord, sprite, itemId);
        _displayedHealth = health;
    }

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

        transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        transform.localPosition = Vector3.zero;
    }

    // ==========================================
    // COORDINATE
    // ==========================================

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
    // DAMAGE VISUAL (Instant, No Animation)
    // ==========================================

    /// <summary>
    /// Apply damage visual instantly. Swaps sprite on this frame.
    /// No squash, no sequence, no timing conflicts with gravity.
    ///
    /// Called directly by AnimationController using pre-gravity coordinates
    /// from the step's DamagedObstacles list — guaranteed to find the right CubeView.
    /// </summary>
    public void ApplyDamageVisual(Sprite damagedSprite)
    {
        if (damagedSprite == null) return;
        SetSprite(damagedSprite);
        SetDefaultSprite(damagedSprite);
        if (_displayedHealth > 0) _displayedHealth--;
    }

    // ==========================================
    // HELPERS
    // ==========================================

    public void SetSortingOrder(int order)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sortingOrder = order;
    }
}
