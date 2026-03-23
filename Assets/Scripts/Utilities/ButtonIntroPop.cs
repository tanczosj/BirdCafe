using System.Collections;
using UnityEngine;

public class ButtonIntroPop : MonoBehaviour
{
    public RectTransform targetButton;
    public Animator buttonAnimator;

    [Header("Scale Multipliers")]
    public float startScale = 0.01f;
    public float popScale = 1.2f;
    public float finalScale = 1f;

    [Header("Timing")]
    public float expandTime = 0.35f;
    public float settleTime = 0.15f;
    public float startDelay = 0f;

    private CanvasGroup canvasGroup;
    private Coroutine introRoutine;
    private Vector3 baseScale;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = targetButton.localScale;
    }

    private void OnEnable()
    {
        if (targetButton == null)
            targetButton = GetComponent<RectTransform>();

        if (introRoutine != null)
            StopCoroutine(introRoutine);

        // Refresh the intended base scale in case you changed it in the editor
        baseScale = targetButton.localScale;

        // Hide during delay
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // Disable animator during intro so it does not fight the pop
        if (buttonAnimator != null)
            buttonAnimator.enabled = false;

        targetButton.localScale = baseScale * startScale;

        introRoutine = StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        canvasGroup.alpha = 1f;
        targetButton.localScale = baseScale * startScale;

        float t = 0f;

        // Step 1: expand past final size
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / expandTime);
            float scaleMul = Mathf.SmoothStep(startScale, popScale, n);
            targetButton.localScale = baseScale * scaleMul;
            yield return null;
        }

        // Step 2: settle to final size
        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / settleTime);
            float scaleMul = Mathf.SmoothStep(popScale, finalScale, n);
            targetButton.localScale = baseScale * scaleMul;
            yield return null;
        }

        // Permanent final size
        targetButton.localScale = baseScale * finalScale;

        canvasGroup.blocksRaycasts = true;

        if (buttonAnimator != null)
            buttonAnimator.enabled = true;

        introRoutine = null;
    }

    private void OnDisable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
    }
}