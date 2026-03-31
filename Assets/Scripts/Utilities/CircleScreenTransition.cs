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
    [SerializeField] private float coveredPause = 0.03f;
    [SerializeField] private bool useUnscaledTime = true;

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

        SetAlpha(1f);

        float maxScale = GetRequiredScaleToCoverScreen();

        yield return AnimateScale(0f, maxScale, growDuration);

        onCovered?.Invoke();

        if (coveredPause > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(coveredPause);
            else
                yield return new WaitForSeconds(coveredPause);
        }

        yield return AnimateScale(maxScale, 0f, shrinkDuration);

        circleRect.localScale = Vector3.zero;
        SetAlpha(0f);

        if (circleImage != null)
            circleImage.raycastTarget = false;

        isPlaying = false;
        onFinished?.Invoke();
    }

    private IEnumerator AnimateScale(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            circleRect.localScale = new Vector3(to, to, 1f);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3f - 2f * t);

            float scale = Mathf.Lerp(from, to, t);
            circleRect.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        circleRect.localScale = new Vector3(to, to, 1f);
    }

    private float GetRequiredScaleToCoverScreen()
    {
        if (rootCanvas == null)
            return 20f;

        RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
        Rect canvasRect = canvasRectTransform.rect;

        float width = canvasRect.width;
        float height = canvasRect.height;

        float requiredDiameter = Mathf.Sqrt(width * width + height * height);

        float baseDiameter = Mathf.Max(originalSize.x, originalSize.y);
        if (baseDiameter <= 0f)
            baseDiameter = 100f;

        return requiredDiameter / baseDiameter;
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