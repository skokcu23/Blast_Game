using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Fail popup with DOTween scale-in animation.
/// Close → MainScene, Try Again → reload level.
/// </summary>
public class FailPopupUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _tryAgainButton;

    [Header("Optional Dark Background")]
    [SerializeField] private CanvasGroup _dimBackground;

    private GameOrchestrator _orchestrator;

    public void Initialize(GameOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;

        _closeButton.onClick.AddListener(OnCloseClicked);
        _tryAgainButton.onClick.AddListener(OnTryAgainClicked);

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Dim background fade in
        if (_dimBackground != null)
        {
            _dimBackground.alpha = 0f;
            DOTween.To(() => _dimBackground.alpha, x => _dimBackground.alpha = x, 0.5f, 0.3f);
        }

        // Popup scale-in with overshoot
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
    }

    private void Hide()
    {
        // Scale out then disable
        transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnCloseClicked()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainScene");
    }

    private void OnTryAgainClicked()
    {
        Hide();

        // Small delay to let the animation play before reloading
        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (_orchestrator != null)
                _orchestrator.ReloadCurrentLevel();
        });
    }
}
