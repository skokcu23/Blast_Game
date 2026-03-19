using UnityEngine;
using DG.Tweening;

/// <summary>
/// Visual representation of a single grid cell.
///
/// Two derived visual systems (both idempotent, both stateless from manager's perspective):
///   Hints:  ApplyHint/RemoveHint — swaps sprite based on group membership
///   Health: SetHealthVisual — animates crack based on obstacle HP
///
/// Both follow the same pattern: the CubeView tracks its current display state
/// internally, and only animates when the state actually changes.
/// </summary>
public class CubeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public string ItemId { get; private set; }
    public Coordinate GridCoordinate { get; private set; }

    private Sprite _defaultSprite;
    private bool _isHinted;
    private int _displayedHealth; // -1 = not tracking (non-obstacle)

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
            _spriteRenderer.sprite = sprite;

        name = $"Cell_{coord.x}_{coord.y}";
    }

    /// <summary>
    /// Configure with initial health tracking (for obstacles like vases).
    /// </summary>
    public void ConfigureWithHealth(Coordinate coord, Sprite sprite, string itemId, int health)
    {
        Configure(coord, sprite, itemId);
        _displayedHealth = health;
    }

    public void Reset()
    {
        DOTween.Kill(transform);
        DOTween.Kill(transform, "hint");
        DOTween.Kill(transform, "damage");

        ItemId = null;
        GridCoordinate = default;
        _defaultSprite = null;
        _isHinted = false;
        _displayedHealth = -1;

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = null;

        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        transform.localPosition = Vector3.zero;
    }

    // ==========================================
    // COORDINATE
    // ==========================================

    public void UpdateCoordinate(Coordinate newCoord)
    {
        GridCoordinate = newCoord;
        name = $"Cell_{newCoord.x}_{newCoord.y}";
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
        transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 1, 0)
            .SetId("hint");
    }

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

    // ==========================================
    // HEALTH VISUAL (Derived State, Idempotent)
    // ==========================================

    /// <summary>
    /// Update the obstacle's visual to match its actual health.
    /// Idempotent: no-op if already displaying this health.
    /// Animates with squash + sprite swap when health changes.
    ///
    /// Call this as many times as you want from any step —
    /// only the first call that detects a change triggers animation.
    /// </summary>
    public void SetHealthVisual(int actualHealth, Sprite correctSprite)
    {
        if (_displayedHealth == actualHealth) return;

        _displayedHealth = actualHealth;

        DOTween.Kill(transform, "damage");

        Sequence seq = DOTween.Sequence();
        seq.SetId("damage");

        // Squash in
        seq.Append(transform
            .DOScale(new Vector3(0.75f, 1.15f, 1f), 0.08f)
            .SetEase(Ease.OutQuad));

        // Sprite swap at peak of squash
        seq.AppendCallback(() =>
        {
            SetSprite(correctSprite);
            SetDefaultSprite(correctSprite);
        });

        // Bounce back
        seq.Append(transform
            .DOScale(new Vector3(1.05f, 0.9f, 1f), 0.08f)
            .SetEase(Ease.OutQuad));

        seq.Append(transform
            .DOScale(new Vector3(0.95f, 0.95f, 1f), 0.12f)
            .SetEase(Ease.OutBounce));
    }
}
