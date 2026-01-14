using UnityEngine;

public class MouseParallax : MonoBehaviour
{
    [SerializeField] private float parallaxStrength = 0.01f;   // Extremely subtle movement
    [SerializeField] private float smoothing = 10f;            // Smooth & slow transition

    private Vector3 initialPos;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        initialPos = transform.position;
    }

    void Update()
    {
        // Get mouse position in world space
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        // Opposite direction movement
        Vector3 offset = (cam.transform.position - mouseWorld) * parallaxStrength;

        // Target parallax position
        Vector3 targetPos = new Vector3(
            initialPos.x + offset.x,
            initialPos.y + offset.y,
            initialPos.z
        );

        // Smooth transition
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * smoothing
        );
    }
}
