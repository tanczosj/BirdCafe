using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircleScreenTransition : MonoBehaviour
{
    [SerializeField] private RectTransform circleRect;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image circleImage;

    [SerializeField] private float growDuration = 0.35f;
    [SerializeField] private float shrinkDuration = 0.35f;
    [SerializeField] private float coveredPause = 0.1f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Size")]
    [SerializeField] private float extraScaleMultiplier = 1.75f;

    [Header("Alpha")]
    [SerializeField][Range(0f, 1f)] private float minAlpha = 0.35f;
    [SerializeField][Range(0f, 1f)] private float maxAlpha = 1f;

    private Vector2 originalSize;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (circleRect == null)
            circleRect = GetComponent<RectTransform>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (circleImage == null)
            circleImage = GetComponent<Image>();

        originalSize = circleRect.sizeDelta;
        circleRect.localScale = Vector3.zero;
        SetAlpha(0f);

        if (circleImage != null)
            circleImage.raycastTarget = false;
    }

    public void Play(Action onCovered, Action onFinished = null)
    {
        if (isPlaying)
            return;

        StartCoroutine(PlayRoutine(onCovered, onFinished));
    }

    private IEnumerator PlayRoutine(Action onCovered, Action onFinished)
    {
        isPlaying = true;

        if (circleImage != null)
            circleImage.raycastTarget = true;

        float maxScale = GetRequiredScaleToCoverScreen();

        // Start slightly transparent.
        SetAlpha(minAlpha);

        // Grow while fading from slightly transparent to fully opaque.
        yield return AnimateScaleAndAlpha(0f, maxScale, minAlpha, maxAlpha, growDuration);

        onCovered?.Invoke();

        if (coveredPause > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(coveredPause);
            else
                yield return new WaitForSeconds(coveredPause);
        }

        // Shrink while fading back to slightly transparent.
        yield return AnimateScaleAndAlpha(maxScale, 0f, maxAlpha, minAlpha, shrinkDuration);

        circleRect.localScale = Vector3.zero;

        // Fully hide it after the animation is done.
        SetAlpha(0f);

        if (circleImage != null)
            circleImage.raycastTarget = false;

        isPlaying = false;
        onFinished?.Invoke();
    }

    private IEnumerator AnimateScaleAndAlpha(float fromScale, float toScale, float fromAlpha, float toAlpha, float duration)
    {
        if (duration <= 0f)
        {
            circleRect.localScale = new Vector3(toScale, toScale, 1f);
            SetAlpha(toAlpha);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3f - 2f * t); // smoothstep easing

            float scale = Mathf.Lerp(fromScale, toScale, t);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            circleRect.localScale = new Vector3(scale, scale, 1f);
            SetAlpha(alpha);

            yield return null;
        }

        circleRect.localScale = new Vector3(toScale, toScale, 1f);
        SetAlpha(toAlpha);
    }

    private float GetRequiredScaleToCoverScreen()
    {
        if (rootCanvas == null)
            return 20f * extraScaleMultiplier;

        RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
        Rect canvasRect = canvasRectTransform.rect;

        float width = canvasRect.width;
        float height = canvasRect.height;

        float requiredDiameter = Mathf.Sqrt(width * width + height * height);

        float baseDiameter = Mathf.Max(originalSize.x, originalSize.y);
        if (baseDiameter <= 0f)
            baseDiameter = 100f;

        return (requiredDiameter / baseDiameter) * extraScaleMultiplier;
    }

    private void SetAlpha(float alpha)
    {
        if (circleImage == null)
            return;

        Color c = circleImage.color;
        c.a = alpha;
        circleImage.color = c;
    }
}