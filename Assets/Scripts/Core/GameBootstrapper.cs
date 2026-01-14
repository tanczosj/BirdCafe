using UnityEngine;
using UnityEngine.SceneManagement;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;

public class GameBootstrapper : MonoBehaviour
{
    private static GameBootstrapper _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        BirdCafeGame.Instance.OnScreenChanged += HandleScreenChange;
    }

    private void OnDestroy()
    {
        BirdCafeGame.Instance.OnScreenChanged -= HandleScreenChange;
    }

    private void HandleScreenChange(GameScreen newScreen)
    {
        string targetScene = GetSceneNameForScreen(newScreen);
        string currentScene = SceneManager.GetActiveScene().name;

        // Only load if the scene is actually changing
        if (targetScene != currentScene)
        {
            // CHANGED: Use the Transition script instead of direct SceneManager
            // We use a 1.0 second fade (0.5s to black, 0.5s to new scene)
            SceneTransition.LoadScene(targetScene, 1.0f, Color.black);
        }
    }

    private string GetSceneNameForScreen(GameScreen screen)
    {
        switch (screen)
        {
            case GameScreen.MainMenu:
            case GameScreen.LoadGame:
                return "MainMenu";

            default:
                return "Gameplay";
        }
    }
}