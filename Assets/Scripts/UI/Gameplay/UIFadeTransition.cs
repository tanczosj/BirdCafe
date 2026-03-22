using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeTransition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fadeImage;

    [Header("Settings")]
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [SerializeField] private bool startTransparent = true;
    [SerializeField] private bool blockRaycastsOnlyWhenVisible = true;
    [SerializeField] private float raycastAlphaThreshold = 0.01f;

    private Coroutine currentFade;

    private void Awake()
    {
        if (fadeImage == null)
            fadeImage = GetComponent<Image>();

        if (fadeImage == null)
        {
            Debug.LogError("UIFadeTransition requires an Image reference.");
            enabled = false;
            return;
        }

        Color c = fadeImage.color;
        c.r = 0f;
        c.g = 0f;
        c.b = 0f;
        c.a = startTransparent ? 0f : 1f;
        fadeImage.color = c;

        UpdateRaycastState();
    }

    public void FadeOutToBlack()
    {
        FadeTo(1f, defaultFadeDuration);
    }

    public void FadeInFromBlack()
    {
        FadeTo(0f, defaultFadeDuration);
    }

    public void FadeOutToBlack(float duration)
    {
        FadeTo(1f, duration);
    }

    public void FadeInFromBlack(float duration)
    {
        FadeTo(0f, duration);
    }

    public void FadeOutThenIn(float fadeOutDuration, float waitTime, float fadeInDuration)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeOutThenInRoutine(fadeOutDuration, waitTime, fadeInDuration));
    }

    private void FadeTo(float targetAlpha, float duration)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;

        if (duration <= 0f)
        {
            color.a = targetAlpha;
            fadeImage.color = color;
            UpdateRaycastState();
            currentFade = null;
            yield break;
        }

        fadeImage.raycastTarget = true;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;
            UpdateRaycastState();

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
        UpdateRaycastState();
        currentFade = null;
    }

    private IEnumerator FadeOutThenInRoutine(float fadeOutDuration, float waitTime, float fadeInDuration)
    {
        yield return FadeRoutine(1f, fadeOutDuration);

        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        yield return FadeRoutine(0f, fadeInDuration);
    }

    private void UpdateRaycastState()
    {
        if (!blockRaycastsOnlyWhenVisible)
        {
            fadeImage.raycastTarget = false;
            return;
        }

        fadeImage.raycastTarget = fadeImage.color.a > raycastAlphaThreshold;
    }
}