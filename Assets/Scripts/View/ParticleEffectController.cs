using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// DOTween-based sprite particle system.
///
/// Instead of Unity's ParticleSystem (URP shader issues in 2D),
/// this creates small sprite GameObjects that burst outward with
/// DOTween animations: random direction, scale down, fade out.
///
/// All particle sprites are pooled — zero garbage collection during gameplay.
/// Matches the particle sprites provided: particle_blue, particle_box_01, etc.
/// </summary>
public class ParticleEffectController
{
    private readonly Transform _parent;
    private readonly Queue<SpriteRenderer> _pool = new Queue<SpriteRenderer>();
    private readonly List<SpriteRenderer> _allInstances = new List<SpriteRenderer>();

    private const int POOL_INITIAL_SIZE = 60;
    private const int GROW_SIZE = 15;

    // --- Burst configuration ---
    private const int CUBE_PARTICLE_COUNT = 5;
    private const int OBSTACLE_PARTICLE_COUNT = 6;
    private const int ROCKET_PARTICLE_COUNT = 8;
    private const int SMOKE_TRAIL_COUNT = 2;
    private const float BURST_RADIUS = 1.2f;
    private const float ROCKET_BURST_RADIUS = 1.8f;
    private const float PARTICLE_DURATION = 0.4f;
    private const float ROCKET_PARTICLE_DURATION = 0.5f;
    private const float SMOKE_DURATION = 0.35f;
    private const float PARTICLE_START_SCALE = 0.4f;
    private const float SMOKE_START_SCALE = 0.3f;
    private const float PARTICLE_GRAVITY = -2f;

    public ParticleEffectController(Transform parent)
    {
        _parent = parent;
        Prewarm(POOL_INITIAL_SIZE);
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// <summary>
    /// Play a burst of particles at a world position using a single sprite.
    /// Used for cube pops (particle_blue, particle_red, etc.)
    /// </summary>
    public void PlayCubeBurst(Vector3 position, Sprite particleSprite)
    {
        if (particleSprite == null) return;

        for (int i = 0; i < CUBE_PARTICLE_COUNT; i++)
        {
            SpawnParticle(position, particleSprite);
        }
    }

    /// <summary>
    /// Play a burst of particles at a world position using multiple sprites.
    /// Used for obstacle destruction (particle_box_01/02/03, etc.)
    /// Each particle randomly picks one of the provided sprites.
    /// </summary>
    public void PlayObstacleBurst(Vector3 position, Sprite[] particleSprites)
    {
        if (particleSprites == null || particleSprites.Length == 0) return;

        for (int i = 0; i < OBSTACLE_PARTICLE_COUNT; i++)
        {
            Sprite sprite = particleSprites[Random.Range(0, particleSprites.Length)];
            if (sprite != null)
                SpawnParticle(position, sprite);
        }
    }

    /// <summary>
    /// Play a large burst at rocket explosion origin using smoke and star sprites.
    /// More particles, wider radius — rockets feel powerful.
    /// </summary>
    public void PlayRocketBurst(Vector3 position, Sprite smokeSprite, Sprite starSprite)
    {
        if (smokeSprite == null && starSprite == null) return;

        for (int i = 0; i < ROCKET_PARTICLE_COUNT; i++)
        {
            // Alternate between smoke and star
            Sprite sprite = (i % 2 == 0 && smokeSprite != null) ? smokeSprite : starSprite;
            if (sprite != null)
                SpawnRocketParticle(position, sprite);
        }
    }

    /// <summary>
    /// Emit a small smoke puff at a position. Used along projectile paths
    /// to create a smoke trail effect behind the rocket parts.
    /// </summary>
    public void PlaySmokeTrail(Vector3 position, Sprite smokeSprite)
    {
        if (smokeSprite == null) return;

        for (int i = 0; i < SMOKE_TRAIL_COUNT; i++)
        {
            SpawnSmokeParticle(position, smokeSprite);
        }
    }

    /// <summary>
    /// Return all active particles to the pool. Call on level load.
    /// </summary>
    public void ReturnAll()
    {
        foreach (var sr in _allInstances)
        {
            if (sr != null && sr.gameObject.activeSelf)
            {
                DOTween.Kill(sr.transform);
                sr.gameObject.SetActive(false);
                _pool.Enqueue(sr);
            }
        }
    }

    // ==========================================
    // PARTICLE SPAWN + ANIMATION
    // ==========================================

    private void SpawnParticle(Vector3 position, Sprite sprite)
    {
        SpriteRenderer sr = Get();
        sr.sprite = sprite;
        sr.color = Color.white;

        Transform t = sr.transform;
        t.position = position;
        t.localScale = Vector3.one * PARTICLE_START_SCALE;

        // Random burst direction
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = Random.Range(0.6f, 1f) * BURST_RADIUS;
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed;

        // Add slight upward bias (particles burst up more than down)
        direction.y += Random.Range(0.3f, 0.8f);

        Vector3 targetPos = position + direction;

        // Apply gravity curve: particle arcs upward then falls
        Vector3 gravityTarget = targetPos + new Vector3(0, PARTICLE_GRAVITY * PARTICLE_DURATION, 0);

        float duration = PARTICLE_DURATION + Random.Range(-0.05f, 0.1f);
        float startScale = PARTICLE_START_SCALE * Random.Range(0.7f, 1.3f);
        float rotation = Random.Range(-180f, 180f);

        t.localScale = Vector3.one * startScale;

        // Animate: move outward + arc down
        t.DOMove(gravityTarget, duration)
            .SetEase(Ease.OutQuad);

        // Scale down to zero
        t.DOScale(Vector3.zero, duration)
            .SetEase(Ease.InQuad);

        // Rotate
        t.DORotate(new Vector3(0, 0, rotation), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        // Fade out
        SpriteRenderer captured = sr;
        DOTween.To(() => captured.color.a, a =>
        {
            if (captured != null)
            {
                var c = captured.color;
                c.a = a;
                captured.color = c;
            }
        }, 0f, duration)
        .SetEase(Ease.InCubic)
        .OnComplete(() => Return(captured));
    }

    /// <summary>
    /// Rocket explosion particle: wider burst, longer duration, bigger scale.
    /// Stars and smoke fly outward dramatically.
    /// </summary>
    private void SpawnRocketParticle(Vector3 position, Sprite sprite)
    {
        SpriteRenderer sr = Get();
        sr.sprite = sprite;
        sr.color = Color.white;

        Transform t = sr.transform;
        t.position = position;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = Random.Range(0.7f, 1f) * ROCKET_BURST_RADIUS;
        Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed;
        direction.y += Random.Range(0.2f, 0.6f);

        Vector3 gravityTarget = position + direction +
            new Vector3(0, PARTICLE_GRAVITY * 0.5f * ROCKET_PARTICLE_DURATION, 0);

        float duration = ROCKET_PARTICLE_DURATION + Random.Range(-0.05f, 0.1f);
        float startScale = PARTICLE_START_SCALE * Random.Range(1.0f, 1.8f);
        float rotation = Random.Range(-270f, 270f);

        t.localScale = Vector3.one * startScale;

        t.DOMove(gravityTarget, duration).SetEase(Ease.OutQuad);
        t.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);
        t.DORotate(new Vector3(0, 0, rotation), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        SpriteRenderer captured = sr;
        DOTween.To(() => captured.color.a, a =>
        {
            if (captured != null)
            {
                var c = captured.color;
                c.a = a;
                captured.color = c;
            }
        }, 0f, duration)
        .SetEase(Ease.InQuad)
        .OnComplete(() => Return(captured));
    }

    /// <summary>
    /// Small smoke puff: drifts slightly, fades quickly, no gravity.
    /// Used along projectile paths for trail effect.
    /// </summary>
    private void SpawnSmokeParticle(Vector3 position, Sprite sprite)
    {
        SpriteRenderer sr = Get();
        sr.sprite = sprite;
        sr.color = new Color(1f, 1f, 1f, 0.7f); // Start slightly transparent

        Transform t = sr.transform;
        t.position = position;

        // Small random drift
        Vector3 drift = new Vector3(
            Random.Range(-0.2f, 0.2f),
            Random.Range(0.1f, 0.4f), // Drifts upward
            0);

        float startScale = SMOKE_START_SCALE * Random.Range(0.8f, 1.2f);
        float duration = SMOKE_DURATION + Random.Range(-0.05f, 0.05f);

        t.localScale = Vector3.one * startScale;

        // Drift outward and expand slightly
        t.DOMove(position + drift, duration).SetEase(Ease.OutQuad);
        t.DOScale(Vector3.one * startScale * 1.5f, duration * 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (t != null)
                    t.DOScale(Vector3.zero, duration * 0.4f).SetEase(Ease.InQuad);
            });

        SpriteRenderer captured = sr;
        DOTween.To(() => captured.color.a, a =>
        {
            if (captured != null)
            {
                var c = captured.color;
                c.a = a;
                captured.color = c;
            }
        }, 0f, duration)
        .SetEase(Ease.InQuad)
        .OnComplete(() => Return(captured));
    }

    // ==========================================
    // POOL
    // ==========================================

    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
            CreateInstance();
    }

    private SpriteRenderer Get()
    {
        if (_pool.Count == 0)
        {
            for (int i = 0; i < GROW_SIZE; i++)
                CreateInstance();
        }

        SpriteRenderer sr = _pool.Dequeue();
        sr.gameObject.SetActive(true);
        return sr;
    }

    private void Return(SpriteRenderer sr)
    {
        if (sr == null) return;
        if (!sr.gameObject.activeSelf) return;

        DOTween.Kill(sr.transform);
        sr.gameObject.SetActive(false);
        _pool.Enqueue(sr);
    }

    private void CreateInstance()
    {
        GameObject go = new GameObject("Particle");
        go.transform.SetParent(_parent);
        go.SetActive(false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 15; // Above everything (cubes=0, projectiles=10)

        _pool.Enqueue(sr);
        _allInstances.Add(sr);
    }
}
