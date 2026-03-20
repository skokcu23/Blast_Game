using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

/// <summary>
/// Win celebration popup with 3-star animation + Continue button.
///
/// Sequence:
///   1. Dim fades in
///   2. PopupBody scales in (OutBack)
///   3. Stars pop in left → center → right
///   4. Continue button fades in
///   5. Player taps Continue → MainScene loads
/// </summary>
public class CelebrationUI : MonoBehaviour
{
    [Header("Dim Background")]
    [SerializeField] private Image _dimBackground;

    [Header("Popup Body")]
    [SerializeField] private RectTransform _popupBody;

    [Header("Stars")]
    [SerializeField] private Image _starLeft;
    [SerializeField] private Image _starCenter;
    [SerializeField] private Image _starRight;

    [Header("Continue Button")]
    [SerializeField] private Button _continueButton;

    private Sequence _celebrationSequence;

    /// <summary>
    /// Play the full celebration sequence. Called by GameOrchestrator on win.
    /// </summary>
    public void PlayCelebration()
    {
        gameObject.SetActive(true);
        ResetVisuals();
        BuildAndPlaySequence();
    }

    private void ResetVisuals()
    {
        if (_dimBackground != null)
        {
            var c = _dimBackground.color;
            c.a = 0f;
            _dimBackground.color = c;
        }

        if (_popupBody != null)
            _popupBody.localScale = Vector3.zero;

        SetStarScale(_starLeft, 0f);
        SetStarScale(_starCenter, 0f);
        SetStarScale(_starRight, 0f);

        // Hide button until stars finish
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(false);
            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void BuildAndPlaySequence()
    {
        _celebrationSequence?.Kill();
        _celebrationSequence = DOTween.Sequence();

        // 1. Dim background fade in
        if (_dimBackground != null)
        {
            _celebrationSequence.Append(
                _dimBackground.DOFade(0.6f, 0.3f).SetEase(Ease.InQuad));
        }

        // 2. Popup body scale in
        if (_popupBody != null)
        {
            _celebrationSequence.Append(
                _popupBody.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
        }

        // 3. Stars pop in one by one
        float starDelay = 0.2f;

        _celebrationSequence.AppendCallback(() => AnimateStar(_starLeft));
        _celebrationSequence.AppendInterval(starDelay);

        _celebrationSequence.AppendCallback(() => AnimateStar(_starCenter));
        _celebrationSequence.AppendInterval(starDelay);

        _celebrationSequence.AppendCallback(() => AnimateStar(_starRight));

        // 4. Wait for last star to finish, then show button
        _celebrationSequence.AppendInterval(0.6f);
        _celebrationSequence.AppendCallback(ShowContinueButton);
    }

    private void ShowContinueButton()
    {
        if (_continueButton == null) return;

        _continueButton.gameObject.SetActive(true);

        // Fade in + slight scale bounce
        CanvasGroup cg = _continueButton.GetComponent<CanvasGroup>();
        RectTransform rt = _continueButton.GetComponent<RectTransform>();

        if (cg != null)
        {
            cg.alpha = 0f;
            cg.DOFade(1f, 0.3f);
        }

        if (rt != null)
        {
            rt.localScale = Vector3.one * 0.8f;
            rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void OnContinueClicked()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainScene");
    }

    private void AnimateStar(Image star)
    {
        if (star == null) return;

        RectTransform rt = star.rectTransform;

        Sequence starSeq = DOTween.Sequence();

        starSeq.Append(
            rt.DOScale(1.4f, 0.2f).SetEase(Ease.OutQuad));

        starSeq.Append(
            rt.DOScale(1f, 0.3f).SetEase(Ease.OutBounce));

        starSeq.Join(
            rt.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f, 4, 0.5f));
    }

    private void SetStarScale(Image star, float scale)
    {
        if (star != null)
            star.rectTransform.localScale = Vector3.one * scale;
    }

    private void OnDestroy()
    {
        _celebrationSequence?.Kill();
    }
}