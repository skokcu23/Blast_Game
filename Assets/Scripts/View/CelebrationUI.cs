using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Win celebration: plays particles, fades dark overlay, then loads MainScene.
///
/// Fix: gameObject.SetActive(true) called BEFORE StartCoroutine.
/// Uses DOTween for smooth overlay fade.
/// </summary>
public class CelebrationUI : MonoBehaviour
{
    [Header("Celebration")]
    [SerializeField] private ParticleSystem _celebrationParticles;
    [SerializeField] private float _celebrationDuration = 2.5f;
    [SerializeField] private float _delayBeforeScene = 1.0f;

    [Header("Overlay")]
    [SerializeField] private CanvasGroup _overlayGroup;

    public void PlayCelebration()
    {
        // MUST enable before starting coroutine
        gameObject.SetActive(true);

        // Reset overlay
        if (_overlayGroup != null)
            _overlayGroup.alpha = 0f;

        StartCoroutine(CelebrationSequence());
    }

    private IEnumerator CelebrationSequence()
    {
        // 1. Start particles
        if (_celebrationParticles != null)
        {
            _celebrationParticles.gameObject.SetActive(true);
            _celebrationParticles.Clear();
            _celebrationParticles.Play();
        }

        // 2. Fade in dark overlay using DOTween
        if (_overlayGroup != null)
        {
            _overlayGroup.gameObject.SetActive(true);
            _overlayGroup.alpha = 0f;
            DOTween.To(() => _overlayGroup.alpha, x => _overlayGroup.alpha = x, 0.6f, 0.5f).SetEase(Ease.InOutQuad);
        }

        // 3. Wait for celebration
        yield return new WaitForSeconds(_celebrationDuration);

        // 4. Fade out everything
        if (_overlayGroup != null)
        {
            DOTween.To(() => _overlayGroup.alpha, x => _overlayGroup.alpha = x, 1f, 0.3f);
        }

        yield return new WaitForSeconds(_delayBeforeScene);

        // 5. Clean up DOTween before scene change
        DOTween.KillAll();

        // 6. Load MainScene
        SceneManager.LoadScene("MainScene");
    }
}
