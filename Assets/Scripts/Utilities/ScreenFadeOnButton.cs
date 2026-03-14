using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFadeOnButton : MonoBehaviour
{
    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;

    [Header("Fade Settings")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeColor;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
    }

    public void PlayFade()
    {
        if (fadeImage == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        yield return FadeAlpha(0f, fadeColor.a > 0f ? fadeColor.a : 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return FadeAlpha(fadeImage.color.a, 0f, fadeOutDuration);

        fadeRoutine = null;
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = duration <= 0f ? 1f : time / duration;

            Color c = fadeColor;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = c;

            yield return null;
        }

        Color finalColor = fadeColor;
        finalColor.a = endAlpha;
        fadeImage.color = finalColor;
    }
}