using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISlamOnEnable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rect;

    [Header("Normal State")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private bool captureNormalScaleFromAwake = true;

    [Header("Slam")]
    [SerializeField] private float startScaleMultiplier = 1.25f;
    [SerializeField] private float startYOffset = 30f;
    [SerializeField] private float duration = 0.14f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Bounce")]
    [SerializeField] private bool addBounce = true;
    [SerializeField] private float bounceScaleMultiplier = 1.06f;
    [SerializeField] private float bounceDuration = 0.06f;

    private Vector2 baseAnchoredPosition;
    private Coroutine currentRoutine;
    private bool initialized;

    private void Awake()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        baseAnchoredPosition = rect.anchoredPosition;

        if (captureNormalScaleFromAwake)
            normalScale = rect.localScale;

        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PlayRoutine());
    }

    public void PlaySlam()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Vector2 endPos = baseAnchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, startYOffset);

        Vector3 endScale = normalScale;
        Vector3 startScale = normalScale * startScaleMultiplier;

        rect.anchoredPosition = startPos;
        rect.localScale = startScale;

        float time = 0f;

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - t, 4f);

            rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);

            yield return null;
        }

        rect.anchoredPosition = endPos;
        rect.localScale = endScale;

        if (addBounce)
        {
            Vector3 bounceScale = endScale * bounceScaleMultiplier;

            time = 0f;
            while (time < bounceDuration)
            {
                time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(time / bounceDuration);
                float eased = Mathf.Sin(t * Mathf.PI);

                rect.localScale = Vector3.LerpUnclamped(endScale, bounceScale, eased * 0.5f);
                yield return null;
            }

            rect.localScale = endScale;
        }

        currentRoutine = null;
    }
}