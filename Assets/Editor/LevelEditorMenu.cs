#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor menu item to set the last played level number.
/// Case Study: "A Unity editor menu item should be implemented to set the last played level number."
///
/// Access via: Tools → Set Level Number
/// </summary>
public class LevelEditorMenu
{
    [MenuItem("Tools/Set Level Number")]
    public static void SetLevelNumber()
    {
        var persistence = new PlayerPrefsLevelPersistence();
        int current = persistence.LoadCurrentLevel();
        LevelEditorWindow.Show(current);
    }
}

/// <summary>
/// Small editor window for inputting the level number.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    private int _levelNumber = 1;

    public static void Show(int currentLevel)
    {
        var window = GetWindow<LevelEditorWindow>("Set Level");
        window._levelNumber = currentLevel;
        window.minSize = new Vector2(250, 100);
        window.maxSize = new Vector2(250, 100);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Enter Level Number (1-10, or 11 for Finished):", EditorStyles.boldLabel);
        GUILayout.Space(5);

        _levelNumber = EditorGUILayout.IntField("Level:", _levelNumber);

        GUILayout.Space(10);

        if (GUILayout.Button("Save"))
        {
            var persistence = new PlayerPrefsLevelPersistence();
            persistence.SaveCurrentLevel(Mathf.Clamp(_levelNumber, 1, 11));
            Debug.Log($"[LevelEditor] Level set to {_levelNumber}");
            Close();
        }
    }
}
#endif
