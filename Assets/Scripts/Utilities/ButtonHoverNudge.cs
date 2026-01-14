using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverNudge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float offsetX = 10f; // how far to move to the right in pixels
    public float moveSpeed = 10f; // how quickly it slides

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        // Smoothly move toward the target position
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Move slightly to the right
        targetPosition = originalPosition + new Vector2(offsetX, 0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Move back to original spot
        targetPosition = originalPosition;
    }
}
