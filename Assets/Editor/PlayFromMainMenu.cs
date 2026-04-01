using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);

        if (mainMenuScene == null)
        {
            Debug.LogError($"MainMenu scene not found at path: {MainMenuScenePath}");
            return;
        }

        EditorSceneManager.playModeStartScene = mainMenuScene;
    }
}