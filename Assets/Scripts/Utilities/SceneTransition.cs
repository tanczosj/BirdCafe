using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Based on the Ricimi Transition script, adapted for Bird Cafe
public class SceneTransition : MonoBehaviour
{
    private static GameObject _canvasContainer;

    public static void LoadScene(string sceneName, float duration = 1.0f, Color? fadeColor = null)
    {
        // 1. Create a dedicated canvas for the fade if it doesn't exist
        if (_canvasContainer == null)
        {
            _canvasContainer = new GameObject("TransitionCanvas");
            var canvas = _canvasContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Ensure it draws on top of everything
            DontDestroyOnLoad(_canvasContainer);
        }

        // 2. Create the fader object
        var fadeObj = new GameObject("TransitionFader");
        fadeObj.transform.SetParent(_canvasContainer.transform, false);
        fadeObj.transform.SetAsLastSibling();

        // 3. Add this script to run the coroutine
        var transition = fadeObj.AddComponent<SceneTransition>();
        transition.StartFade(sceneName, duration, fadeColor ?? Color.black);
    }

    private void StartFade(string sceneName, float duration, Color color)
    {
        StartCoroutine(RunFadeRoutine(sceneName, duration, color));
    }

    private IEnumerator RunFadeRoutine(string sceneName, float duration, Color color)
    {
        // --- SETUP IMAGE ---
        var image = gameObject.AddComponent<Image>();
        image.color = new Color(color.r, color.g, color.b, 0); // Start Transparent

        // Make it cover the whole screen
        var rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        float halfDuration = duration / 2.0f;

        // --- FADE IN (To Black) ---
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / halfDuration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        image.color = new Color(color.r, color.g, color.b, 1);

        // --- LOAD SCENE ---
        // We wait one frame to ensure the black screen is fully rendered
        yield return null;
        SceneManager.LoadScene(sceneName);

        // --- FADE OUT (From Black) ---
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1.0f - Mathf.Clamp01(timer / halfDuration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // --- CLEANUP ---
        Destroy(gameObject); // Destroy the fader object

        // If no other faders are running, we could destroy the canvas, 
        // but keeping it alive for the next transition is fine too.
    }
}