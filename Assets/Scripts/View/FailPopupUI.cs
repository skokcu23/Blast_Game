using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Fail popup shown when the player runs out of moves.
///
/// Case Study: "A fail popup should be shown to the user, which has options to
/// return to MainScene with a close button and replay the level with a try again button."
/// </summary>
public class FailPopupUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _tryAgainButton;

    private GameOrchestrator _orchestrator;

    /// <summary>
    /// Initialize with orchestrator reference for replay functionality.
    /// </summary>
    public void Initialize(GameOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;

        _closeButton.onClick.AddListener(OnCloseClicked);
        _tryAgainButton.onClick.AddListener(OnTryAgainClicked);

        // Start hidden
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show the popup with an animation.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);

        // Simple scale-in animation
        transform.localScale = Vector3.zero;
        StartCoroutine(ScaleIn());
    }

    private System.Collections.IEnumerator ScaleIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Overshoot ease
            float scale = t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            transform.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private void OnCloseClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    private void OnTryAgainClicked()
    {
        gameObject.SetActive(false);

        if (_orchestrator != null)
            _orchestrator.ReloadCurrentLevel();
    }
}
