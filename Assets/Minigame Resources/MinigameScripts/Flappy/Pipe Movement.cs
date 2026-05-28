using UnityEngine;

public class PipeMove : MonoBehaviour
{
    public float speed = 3f;
    public float despawnPadding = 0.5f;

    private Camera mainCamera;
    private Renderer[] renderers;

    void Start()
    {
        mainCamera = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (GameManager.IsGameOver)
            return;

        transform.position += Vector3.left * speed * Time.deltaTime;

        if (IsFullyOffscreenLeft())
        {
            Destroy(gameObject);
        }
    }

    bool IsFullyOffscreenLeft()
    {
        if (mainCamera == null || renderers == null || renderers.Length == 0)
            return transform.position.x < -10f;

        float cameraLeftEdge = mainCamera.transform.position.x
            - mainCamera.orthographicSize * mainCamera.aspect;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.max.x < cameraLeftEdge - despawnPadding;
    }
}
