using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays celebration particles and animation when the level is won,
/// then transitions back to MainScene.
///
/// Case Study: "Celebration particles and animation should be shown to the user.
/// MainScene should be loaded."
/// </summary>
public class CelebrationUI : MonoBehaviour
{
    [Header("Celebration")]
    [SerializeField] private ParticleSystem _celebrationParticles;
    [SerializeField] private float _celebrationDuration = 2.5f;
    [SerializeField] private float _delayBeforeScene = 1.0f;

    [Header("Optional: Overlay")]
    [SerializeField] private CanvasGroup _overlayGroup; // Fades in during celebration

    /// <summary>
    /// Play the win celebration, then load MainScene.
    /// </summary>
    public void PlayCelebration()
    {
        // Must enable the GameObject BEFORE starting the coroutine
        gameObject.SetActive(true);
        StartCoroutine(CelebrationSequence());
    }

    private IEnumerator CelebrationSequence()
    {
        // 1. Start particles
        if (_celebrationParticles != null)
        {
            _celebrationParticles.gameObject.SetActive(true);
            _celebrationParticles.Play();
        }

        // 2. Fade in overlay if present
        if (_overlayGroup != null)
        {
            _overlayGroup.gameObject.SetActive(true);
            _overlayGroup.alpha = 0f;

            float fadeTime = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                _overlayGroup.alpha = elapsed / fadeTime;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _overlayGroup.alpha = 1f;
        }

        // 3. Wait for celebration to finish
        yield return new WaitForSeconds(_celebrationDuration);

        // 4. Brief pause then load MainScene
        yield return new WaitForSeconds(_delayBeforeScene);

        SceneManager.LoadScene("MainScene");
    }
}
