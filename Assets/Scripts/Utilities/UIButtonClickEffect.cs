using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 pressedScale = new Vector3(0.9f, 0.9f, 0.9f);
    [SerializeField] private float pressSpeed = 18f;
    [SerializeField] private float releaseBounceScale = 1.05f;
    [SerializeField] private float bounceDuration = 0.08f;

    private RectTransform rectTransform;
    private Vector3 targetScale;
    private Coroutine bounceCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.localScale = normalScale;
        targetScale = normalScale;
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * pressSpeed
        );
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (bounceCoroutine != null)
            StopCoroutine(bounceCoroutine);

        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (bounceCoroutine != null)
            StopCoroutine(bounceCoroutine);

        bounceCoroutine = StartCoroutine(ReleaseBounce());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (bounceCoroutine != null)
            StopCoroutine(bounceCoroutine);

        targetScale = normalScale;
    }

    private IEnumerator ReleaseBounce()
    {
        targetScale = normalScale * releaseBounceScale;
        yield return new WaitForSecondsRealtime(bounceDuration);

        targetScale = normalScale;
        bounceCoroutine = null;
    }
}