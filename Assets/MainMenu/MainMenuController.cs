using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplayScene;
#endif

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float blackHoldTime = 0.5f;

    private bool isLoading;

    private void Awake()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.interactable = false;
            fadeOverlay.blocksRaycasts = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameplayScene != null)
            gameplaySceneName = gameplayScene.name;
    }
#endif

    public void PlayGame()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("No gameplay scene assigned.");
            return;
        }

        StartCoroutine(FadeHoldAndLoadScene());
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeHoldAndLoadScene()
    {
        isLoading = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.interactable = true;
            fadeOverlay.blocksRaycasts = true;

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeDuration);
                fadeOverlay.alpha = t;
                yield return null;
            }

            fadeOverlay.alpha = 1f;
        }

        if (blackHoldTime > 0f)
            yield return new WaitForSeconds(blackHoldTime);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(gameplaySceneName);

        if (loadOp == null)
        {
            Debug.LogError($"Could not load scene '{gameplaySceneName}'. Make sure it is in Build Settings.");
            isLoading = false;

            if (fadeOverlay != null)
            {
                fadeOverlay.interactable = false;
                fadeOverlay.blocksRaycasts = false;
            }

            yield break;
        }

        while (!loadOp.isDone)
            yield return null;
    }
}