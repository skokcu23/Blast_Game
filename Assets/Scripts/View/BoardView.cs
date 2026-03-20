using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// Thin facade coordinating the three view subsystems:
///   CubePool         — object pooling for CubeViews
///   GridStateManager — single owner of visual state (_cells dictionary)
///   AnimationController — purely cosmetic DOTween animations
///
/// The Orchestrator only talks to BoardView. BoardView delegates everything.
///
/// Also holds all sprite references (SerializedFields) and provides
/// lookup methods used by the subsystems.

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

    [Header("Cube Particle Sprites")]
    [SerializeField] private Sprite _particleBlue;
    [SerializeField] private Sprite _particleRed;
    [SerializeField] private Sprite _particleGreen;
    [SerializeField] private Sprite _particleYellow;

    [Header("Box Particle Sprites")]
    [SerializeField] private Sprite _particleBox01;
    [SerializeField] private Sprite _particleBox02;
    [SerializeField] private Sprite _particleBox03;

    [Header("Stone Particle Sprites")]
    [SerializeField] private Sprite _particleStone01;
    [SerializeField] private Sprite _particleStone02;
    [SerializeField] private Sprite _particleStone03;

    [Header("Vase Particle Sprites")]
    [SerializeField] private Sprite _particleVase01;
    [SerializeField] private Sprite _particleVase02;
    [SerializeField] private Sprite _particleVase03;

    [Header("Rocket Particle Sprites")]
    [SerializeField] private Sprite _particleSmoke;
    [SerializeField] private Sprite _particleStar;

    [Header("Board Frame")]
    [SerializeField] private SpriteRenderer _boardFrame;

    // Public accessors for AnimationController
    public Sprite HorizontalPartLeftSprite => _horizontalPartLeftSprite;
    public Sprite HorizontalPartRightSprite => _horizontalPartRightSprite;
    public Sprite VerticalPartTopSprite => _verticalPartTopSprite;
    public Sprite VerticalPartBottomSprite => _verticalPartBottomSprite;

    public static readonly Vector3 CellScale = new Vector3(1.3f, 1.3f, 1f);

    public int BoardHeight { get; private set; }

    // Subsystems
    private CubePool _pool;
    private GridStateManager _gridState;
    private AnimationController _animator;
    private ParticleEffectController _particles;

    private float _offsetX;
    private float _offsetY;

    private void Awake()
    {
        _pool = new CubePool(_cubePrefab, transform);
        _gridState = new GridStateManager(_pool, this);
        _particles = new ParticleEffectController(transform);
        _animator = new AnimationController(_gridState, this, _particles, transform);
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(Board board)
    {
        DOTween.KillAll();

        _offsetX = (board.Width - 1) / 2f;
        _offsetY = (board.Height - 1) / 2f;
        BoardHeight = board.Height;  // add this line

        _gridState.Clear();
        _particles.ReturnAll();
        _pool.Prewarm(board.Width * board.Height + 20);
        _gridState.SpawnAll(board);

        FitCameraToBoard(board.Width, board.Height);
    }




    // ==========================================
    // ANIMATION API
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

    public float RocketSpawnPause => _animator.RocketSpawnPause;

    public async Task AnimateRocketExplosion(RocketExplosionData data)
    {
        await _animator.PlayRocketExplosion(data);
    }

    public async Task AnimateComboExplosion(List<RocketExplosionData> explosions)
    {
        await _animator.PlayComboExplosion(explosions);
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
    // PARTICLE SPRITE LOOKUPS
    // ==========================================

    /// <summary>
    /// Get the particle sprite for a cube color.
    /// Returns null for non-cube items.
    /// </summary>
    public Sprite GetCubeParticleSprite(string itemId) => itemId switch
    {
        ItemIds.Blue => _particleBlue,
        ItemIds.Red => _particleRed,
        ItemIds.Green => _particleGreen,
        ItemIds.Yellow => _particleYellow,
        _ => null,
    };

    /// <summary>
    /// Get the particle sprites for an obstacle type.
    /// Returns array of fragment sprites (randomly picked per particle).
    /// Returns null for non-obstacle items.
    /// </summary>
    public Sprite[] GetObstacleParticleSprites(string itemId) => itemId switch
    {
        ItemIds.Box => new[] { _particleBox01, _particleBox02, _particleBox03 },
        ItemIds.Stone => new[] { _particleStone01, _particleStone02, _particleStone03 },
        ItemIds.Vase => new[] { _particleVase01, _particleVase02, _particleVase03 },
        _ => null,
    };

    /// <summary>Smoke sprite for rocket trails and explosions.</summary>
    public Sprite ParticleSmoke => _particleSmoke;

    /// <summary>Star sprite for rocket explosion bursts.</summary>
    public Sprite ParticleStar => _particleStar;

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


    }
}
