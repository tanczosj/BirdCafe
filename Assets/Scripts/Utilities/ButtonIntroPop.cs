using UnityEngine;

public class ButtonIntroPop : MonoBehaviour
{
    public RectTransform targetButton;
    public Animator buttonAnimator;

    public float startScale = 0.01f; // extremely small
    public float popScale = 1.2f;   // overshoot scale
    public float finalScale = 1f;   // normal scale

    public float expandTime = 0.35f;
    public float settleTime = 0.15f;

    public float startDelay = 0f; // new delay before animation starts

    private void OnEnable()
    {
        if (targetButton == null)
            targetButton = GetComponent<RectTransform>();

        if (buttonAnimator == null)
            buttonAnimator = GetComponent<Animator>();

        // Disable Animator so it doesn't override scale
        if (buttonAnimator != null)
            buttonAnimator.enabled = false;

        // Start tiny
        targetButton.localScale = Vector3.one * startScale;

        StartCoroutine(PlayIntro());
    }

    private System.Collections.IEnumerator PlayIntro()
    {
        // Step 0 — Wait for start delay
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float t = 0f;

        // Step 1 — Pop outward
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / expandTime);
            float scale = Mathf.SmoothStep(startScale, popScale, n);
            targetButton.localScale = Vector3.one * scale;
            yield return null;
        }

        // Step 2 — Settle back to 1.0
        t = 0f;
        while (t < settleTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / settleTime);
            float scale = Mathf.SmoothStep(popScale, finalScale, n);
            targetButton.localScale = Vector3.one * scale;
            yield return null;
        }

        // Set final scale
        targetButton.localScale = Vector3.one * finalScale;

        // Step 3 — Re-enable your button animation
        if (buttonAnimator != null)
            buttonAnimator.enabled = true;
    }
}
