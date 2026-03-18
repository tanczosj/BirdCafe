using UnityEngine;

public class CartoonBob2D : MonoBehaviour
{
    [Header("Bob")]
    [SerializeField] private float bobHeight = 8f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationAmount = 3f;
    [SerializeField] private float rotationSpeed = 1.5f;

    [Header("Position Space")]
    [SerializeField] private bool useLocalPosition = true;

    [Header("Parallax")]
    [SerializeField] private float maxParallaxOffset = 30f;
    [SerializeField] private float parallaxSmoothSpeed = 5f;
    [SerializeField] private bool invertParallax = true;

    [Header("Hover Scale")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    [SerializeField] private float scaleSmoothSpeed = 8f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 startScale;
    private float seed;
    private Vector3 currentParallaxOffset;
    private bool isHovered;

    private void Awake()
    {
        startPos = useLocalPosition ? transform.localPosition : transform.position;
        startRot = transform.localRotation;
        startScale = transform.localScale;
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time + seed;

        // Bobbing
        float y = Mathf.Sin(t * bobSpeed) * bobHeight;

        // Rotation
        float rotZ = Mathf.Sin(t * rotationSpeed) * rotationAmount;

        // Mouse position normalized to -0.5 to +0.5
        Vector3 mousePos = Input.mousePosition;
        float nx = (mousePos.x / Screen.width) - 0.5f;
        float ny = (mousePos.y / Screen.height) - 0.5f;

        float direction = invertParallax ? -1f : 1f;

        Vector3 targetParallaxOffset = new Vector3(
            nx * maxParallaxOffset * 2f * direction,
            ny * maxParallaxOffset * 2f * direction,
            0f
        );

        currentParallaxOffset = Vector3.Lerp(
            currentParallaxOffset,
            targetParallaxOffset,
            Time.deltaTime * parallaxSmoothSpeed
        );

        Vector3 finalPos = startPos + currentParallaxOffset + new Vector3(0f, y, 0f);

        if (useLocalPosition)
            transform.localPosition = finalPos;
        else
            transform.position = finalPos;

        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, rotZ);

        // Hover scaling
        Vector3 targetScale = isHovered
            ? startScale * hoverScaleMultiplier
            : startScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSmoothSpeed
        );
    }

    private void OnMouseEnter()
    {
        isHovered = true;
    }

    private void OnMouseExit()
    {
        isHovered = false;
    }
}