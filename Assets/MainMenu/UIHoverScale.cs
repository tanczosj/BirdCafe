using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.15f, 1.15f, 1.15f);
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool debugLogs = false;

    private Vector3 currentTargetScale;
    private bool isHovered;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (target != null)
        {
            normalScale = target.localScale;
            currentTargetScale = normalScale;
        }
    }

    private void Update()
    {
        if (target == null)
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = 1f - Mathf.Exp(-smoothSpeed * dt);
        target.localScale = Vector3.Lerp(target.localScale, currentTargetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        currentTargetScale = hoverScale;

        if (debugLogs)
            Debug.Log($"{name} hover enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        currentTargetScale = normalScale;

        if (debugLogs)
            Debug.Log($"{name} hover exit");
    }
}