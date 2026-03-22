using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverSlideImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Image to Move")]
    [SerializeField] private RectTransform targetImage;

    [Header("Positions")]
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 shownPosition;

    [Header("Animation")]
    [SerializeField] private float moveSpeed = 10f;

    private Vector2 targetPosition;

    private void Awake()
    {
        if (targetImage != null)
        {
            targetImage.anchoredPosition = hiddenPosition;
            targetPosition = hiddenPosition;
        }
    }

    private void Update()
    {
        if (targetImage == null)
            return;

        targetImage.anchoredPosition = Vector2.Lerp(
            targetImage.anchoredPosition,
            targetPosition,
            Time.unscaledDeltaTime * moveSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = shownPosition;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = hiddenPosition;
    }
}