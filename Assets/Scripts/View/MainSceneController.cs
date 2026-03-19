using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the MainScene.
///
/// Case Study requirements:
///   - LevelButton displays current level number
///   - When all levels finished, LevelButton shows "Finished"
///   - Background is an area image
///   - Tapping LevelButton loads LevelScene
/// </summary>
public class MainSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _levelButton;
    [SerializeField] private TextMeshProUGUI _levelButtonText;

    private LevelProgressionManager _progression;

    void Start()
    {
        var persistence = new PlayerPrefsLevelPersistence();
        int totalLevels = LevelParser.GetTotalLevelCount();
        _progression = new LevelProgressionManager(persistence, totalLevels);

        // Update button text
        _levelButtonText.text = _progression.GetLevelButtonText();

        // Wire button
        _levelButton.onClick.AddListener(OnLevelButtonClicked);

        // Disable button if all levels complete
        if (_progression.AllLevelsComplete)
        {
            _levelButton.onClick.RemoveAllListeners();
        }
    }

    private void OnLevelButtonClicked()
    {
        if (_progression.AllLevelsComplete)
            return;

        SceneManager.LoadScene("LevelScene");
    }
}
