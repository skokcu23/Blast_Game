using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Thin facade coordinating the three view subsystems.
///
/// Animation API is clean — no Board, no health snapshots, no damage step.
/// Each animation method receives only the DTO it needs.
/// Damage visuals handled internally by AnimationController.CrackDamagedVases.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject _cubePrefab;

    [Header("Cube Sprites")]
    [SerializeField] private Sprite _blueSprite;
    [SerializeField] private Sprite _redSprite;
    [SerializeField] private Sprite _yellowSprite;
    [SerializeField] private Sprite _greenSprite;

    [Header("Rocket Hint Sprites")]
    [SerializeField] private Sprite _redRocketHint;
    [SerializeField] private Sprite _blueRocketHint;
    [SerializeField] private Sprite _greenRocketHint;
    [SerializeField] private Sprite _yellowRocketHint;

    [Header("Obstacle Sprites")]
    [SerializeField] private Sprite _boxSprite;
    [SerializeField] private Sprite _stoneSprite;
    [SerializeField] private Sprite _vaseSprite;
    [SerializeField] private Sprite _vaseDamagedSprite;

    [Header("Rocket Sprites")]
    [SerializeField] private Sprite _verticalRocketSprite;
    [SerializeField] private Sprite _horizontalRocketSprite;

    [Header("Rocket Part Sprites")]
    [SerializeField] private Sprite _horizontalPartLeftSprite;
    [SerializeField] private Sprite _horizontalPartRightSprite;
    [SerializeField] private Sprite _verticalPartTopSprite;
    [SerializeField] private Sprite _verticalPartBottomSprite;

    [Header("Background")]
    [SerializeField] private SpriteRenderer _levelBackground;

    public Sprite HorizontalPartLeftSprite => _horizontalPartLeftSprite;
    public Sprite HorizontalPartRightSprite => _horizontalPartRightSprite;
    public Sprite VerticalPartTopSprite => _verticalPartTopSprite;
    public Sprite VerticalPartBottomSprite => _verticalPartBottomSprite;

    public static readonly Vector3 CellScale = new Vector3(0.95f, 0.95f, 1f);

    private CubePool _pool;
    private GridStateManager _gridState;
    private AnimationController _animator;

    private float _offsetX;
    private float _offsetY;

    private void Awake()
    {
        _pool = new CubePool(_cubePrefab, transform);
        _gridState = new GridStateManager(_pool, this);
        _animator = new AnimationController(_gridState, this, transform);
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(Board board)
    {
        DOTween.KillAll();

        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;

        _gridState.Clear();
        _pool.Prewarm(board.Width * board.Height + 20);
        _gridState.SpawnAll(board);

        FitCameraToBoard(board.Width, board.Height);
    }

    // ==========================================
    // ANIMATION API — Clean, no Board, no snapshots
    // ==========================================

    public async Task AnimateBlast(BlastResult result)
    {
        await _animator.PlayBlast(result);
    }

    public async Task AnimateRocketCreation(BlastResult blastResult)
    {
        await _animator.PlayRocketCreation(blastResult);
    }

    public void SpawnRocketVisual(RocketCreationData data)
    {
        _animator.PlayRocketSpawn(data);
    }

    public async Task AnimateRocketExplosion(RocketExplosionData data)
    {
        await _animator.PlayRocketExplosion(data);
    }

    public async Task AnimateGravity(List<ItemMovement> movements)
    {
        await _animator.PlayGravity(movements);
    }

    public async Task AnimateRefill(List<ItemMovement> newItems)
    {
        await _animator.PlayRefill(newItems);
    }

    // ==========================================
    // RECONCILIATION & HINTS
    // ==========================================

    public void ReconcileWithBoard(Board board)
    {
        _gridState.SyncWithBoard(board);
    }

    public void UpdateRocketHints(Dictionary<Coordinate, string> hints)
    {
        _gridState.UpdateHints(hints);
    }

    // ==========================================
    // COORDINATE CONVERSION
    // ==========================================

    public Vector3 GridToWorld(Coordinate coord)
    {
        return new Vector3(coord.x - _offsetX, coord.y - _offsetY, 0);
    }

    // ==========================================
    // SPRITE LOOKUPS
    // ==========================================

    public Sprite GetSpriteForItem(string itemId) => itemId switch
    {
        ItemIds.Blue => _blueSprite,
        ItemIds.Red => _redSprite,
        ItemIds.Yellow => _yellowSprite,
        ItemIds.Green => _greenSprite,
        ItemIds.Box => _boxSprite,
        ItemIds.Stone => _stoneSprite,
        ItemIds.Vase => _vaseSprite,
        ItemIds.VerticalRocket => _verticalRocketSprite,
        ItemIds.HorizontalRocket => _horizontalRocketSprite,
        _ => null,
    };

    public Sprite GetVaseSprite(int health) =>
        (health <= 1 && _vaseDamagedSprite != null) ? _vaseDamagedSprite : _vaseSprite;

    public Sprite GetHintSpriteForColor(string itemId) => itemId switch
    {
        ItemIds.Red => _redRocketHint,
        ItemIds.Blue => _blueRocketHint,
        ItemIds.Green => _greenRocketHint,
        ItemIds.Yellow => _yellowRocketHint,
        _ => null,
    };

    // ==========================================
    // CAMERA & BACKGROUND
    // ==========================================

    private void FitCameraToBoard(int boardWidth, int boardHeight)
    {
        if (Camera.main == null) return;

        float topUIPadding = 3.5f;
        float bottomPadding = 0.5f;
        float requiredHeight = boardHeight + topUIPadding + bottomPadding;

        float aspectRatio = (float)Screen.width / Screen.height;
        float requiredWidth = boardWidth + 1f;
        float heightFromWidth = (requiredWidth / aspectRatio) / 2f;
        float orthoSize = Mathf.Max(requiredHeight / 2f, heightFromWidth);

        Camera.main.orthographicSize = orthoSize;

        float verticalOffset = (topUIPadding - bottomPadding) / 2f;
        Camera.main.transform.position = new Vector3(0, verticalOffset, -10f);

        if (_levelBackground != null && _levelBackground.sprite != null)
        {
            float cameraHeight = Camera.main.orthographicSize * 2f;
            float cameraWidth = cameraHeight * Camera.main.aspect;
            Vector2 spriteSize = _levelBackground.sprite.bounds.size;

            float fillScale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
            _levelBackground.transform.localScale = new Vector3(fillScale, fillScale, 1f);
            _levelBackground.transform.position = new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y,
                1f
            );
        }
    }
}
