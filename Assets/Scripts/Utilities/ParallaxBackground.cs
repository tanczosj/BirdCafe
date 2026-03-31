using UnityEngine;
using UnityEngine.InputSystem;

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

        maxOffset /= 2f;
    }

    private void Update()
    {
        // No mouse available
        if (Mouse.current == null)
            return;

        // Mouse position in screen space
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Normalize to range -0.5 .. +0.5 (center of screen is 0,0)
        float nx = (mousePos.x / Screen.width) - 0.5f;
        float ny = (mousePos.y / Screen.height) - 0.5f;

        // Move in the opposite direction of the mouse
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