using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// Win celebration: shows "Level Completed!" text with animation,
/// fades screen dark, then loads MainScene.
/// </summary>
public class CelebrationUI : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private Image _fadeImage;

    [Header("Win Text (optional)")]
    [SerializeField] private TextMeshProUGUI _winText;

    [Header("Timing")]
    [SerializeField] private float _textAppearDelay = 0.3f;
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _holdDuration = 1.5f;

    public void PlayCelebration()
    {
        gameObject.SetActive(true);

        // Reset state
        if (_fadeImage != null)
        {
            var c = _fadeImage.color;
            c.a = 0f;
            _fadeImage.color = c;
        }

        if (_winText != null)
        {
            _winText.transform.localScale = Vector3.zero;
            _winText.gameObject.SetActive(false);
        }

        StartCoroutine(CelebrationSequence());
    }

    private IEnumerator CelebrationSequence()
    {
        // 1. Quick semi-transparent overlay fade
        if (_fadeImage != null)
        {
            _fadeImage.DOFade(0.5f, 0.4f).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(_textAppearDelay);

        // 2. Show win text with bounce-in
        if (_winText != null)
        {
            _winText.gameObject.SetActive(true);
            _winText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(1.0f);

        // 3. Full fade to black
        if (_fadeImage != null)
        {
            _fadeImage.DOFade(1f, _fadeInDuration).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(_fadeInDuration + _holdDuration);

        // 4. Load MainScene
        DOTween.KillAll();
        SceneManager.LoadScene("MainScene");
    }
}