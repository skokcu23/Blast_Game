using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for a single goal item in the top bar.
/// Shows: obstacle icon, remaining count, checkmark when cleared.
/// </summary>
public class GoalItemUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Image _checkOverlay;

    private Sprite _checkSprite;

    public void Setup(Sprite icon, int initialCount, Sprite checkSprite)
    {
        _checkSprite = checkSprite;

        if (_iconImage != null)
            _iconImage.sprite = icon;

        if (_checkOverlay != null)
        {
            if (_checkSprite != null)
                _checkOverlay.sprite = _checkSprite;
            _checkOverlay.gameObject.SetActive(false);
        }

        UpdateCount(initialCount);
    }

    public void UpdateCount(int remaining)
    {
        if (remaining <= 0)
        {
            if (_countText != null)
                _countText.gameObject.SetActive(false);

            if (_checkOverlay != null)
                _checkOverlay.gameObject.SetActive(true);
        }
        else
        {
            if (_countText != null)
            {
                _countText.gameObject.SetActive(true);
                _countText.text = remaining.ToString();
            }

            if (_checkOverlay != null)
                _checkOverlay.gameObject.SetActive(false);
        }
    }
}
