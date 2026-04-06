using UnityEngine;
using UnityEngine.SceneManagement;
using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
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

        if (targetScene != currentScene)
        {
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

            case GameScreen.Minigame:
                return ResolveMinigameSceneName();

            default:
                return "Gameplay";
        }
    }

    private string ResolveMinigameSceneName()
    {
        var session = BirdCafeGame.Instance.GetCurrentMinigameSession();
        if (session == null)
            return "Gameplay";

        switch (session.Minigame)
        {
            case MinigameId.TimingBarGame:
                return "TimingGame";

            case MinigameId.Flappy:
            default:
                return "Flappy";
        }
    }
}