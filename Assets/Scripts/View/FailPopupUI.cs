using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Fail popup with smooth DOTween animations.
/// Close → MainScene, Try Again → reload level.
/// </summary>
public class FailPopupUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _tryAgainButton;

    [Header("Dim Background")]
    [SerializeField] private Image _dimBackground; // Full-screen semi-transparent black

    [Header("Popup Body")]
    [SerializeField] private RectTransform _popupBody; // The popup panel (not the full-screen container)

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

        // Fade in dim background
        if (_dimBackground != null)
        {
            var c = _dimBackground.color;
            c.a = 0f;
            _dimBackground.color = c;
            _dimBackground.DOFade(0.6f, 0.3f).SetEase(Ease.InQuad);
        }

        // Scale-in popup body with overshoot
        if (_popupBody != null)
        {
            _popupBody.localScale = Vector3.zero;
            _popupBody.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void Hide(System.Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        // Scale out popup
        if (_popupBody != null)
        {
            seq.Join(_popupBody.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
        }

        // Fade out dim background
        if (_dimBackground != null)
        {
            seq.Join(_dimBackground.DOFade(0f, 0.2f));
        }

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void OnCloseClicked()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainScene");
    }

    private void OnTryAgainClicked()
    {
        Hide(() =>
        {
            if (_orchestrator != null)
                _orchestrator.ReloadCurrentLevel();
        });
    }
}