using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HudDayCycleTint : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float dayLength = 120f; // seconds for a full day
    [SerializeField] private bool loop = true;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Visual Settings")]
    [SerializeField] private Gradient colorOverDay;
    [SerializeField]
    private AnimationCurve brightnessOverDay = new AnimationCurve(
        new Keyframe(0f, 0.35f),   // midnight
        new Keyframe(0.25f, 0.75f),// morning
        new Keyframe(0.5f, 1f),    // noon
        new Keyframe(0.75f, 0.65f),// evening
        new Keyframe(1f, 0.35f)    // midnight
    );

    [SerializeField] private float overlayAlpha = 0.35f;

    private Image hudImage;
    private float timer;

    private void Awake()
    {
        hudImage = GetComponent<Image>();

        // Default gradient if none is set in inspector
        if (colorOverDay.colorKeys.Length == 0)
        {
            Gradient defaultGradient = new Gradient();
            defaultGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.08f, 0.12f, 0.25f), 0f),   // midnight blue
                    new GradientColorKey(new Color(1f, 0.55f, 0.3f), 0.2f),    // sunrise orange
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),         // noon
                    new GradientColorKey(new Color(1f, 0.45f, 0.25f), 0.75f),  // sunset
                    new GradientColorKey(new Color(0.08f, 0.12f, 0.25f), 1f)   // night
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            colorOverDay = defaultGradient;
        }
    }

    private void Update()
    {
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        timer += delta;

        float t = timer / dayLength;

        if (loop)
        {
            t %= 1f;
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        ApplyVisuals(t);
    }

    private void ApplyVisuals(float t)
    {
        Color dayColor = colorOverDay.Evaluate(t);
        float brightness = Mathf.Clamp01(brightnessOverDay.Evaluate(t));

        // Multiply color by brightness
        Color finalColor = new Color(
            dayColor.r * brightness,
            dayColor.g * brightness,
            dayColor.b * brightness,
            overlayAlpha
        );

        hudImage.color = finalColor;
    }

    // Optional: lets you set time manually from other scripts
    public void SetTimeNormalized(float t)
    {
        t = Mathf.Clamp01(t);
        timer = t * dayLength;
        ApplyVisuals(t);
    }
}