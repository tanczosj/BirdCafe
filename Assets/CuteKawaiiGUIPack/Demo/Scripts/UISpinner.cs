using UnityEngine;

public class UISpinner : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool ignoreTimeScale = false;

    private RectTransform rectTransform;
    private float currentAngle;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            currentAngle = rectTransform.localEulerAngles.z;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        float deltaTime = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        float direction = clockwise ? -1f : 1f;

        currentAngle += degreesPerSecond * direction * deltaTime;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}