using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Max distance (in pixels) the background will move from its original position.")]
    public float maxOffset = 30f;

    [Tooltip("How quickly the background follows the mouse.")]
    public float smoothSpeed = 5f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        // Mouse position in screen space
        Vector3 mousePos = Input.mousePosition;

        // Normalize to range -0.5 .. +0.5 (center of screen is 0,0)
        float nx = (mousePos.x / Screen.width) - 0.5f;
        float ny = (mousePos.y / Screen.height) - 0.5f;

        // Move in the OPPOSITE direction of the mouse
        Vector2 offset = new Vector2(-nx, -ny) * maxOffset * 2f;
        Vector2 targetPos = originalPosition + offset;

        // Smoothly move toward target
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }
}
