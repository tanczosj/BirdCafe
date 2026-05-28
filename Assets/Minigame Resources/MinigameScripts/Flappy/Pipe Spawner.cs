using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipePrefab;
    public float spawnRate = 2f;
    public float heightOffset = 2f;
    public float initialSpawnDelay = 0.25f;
    public float spawnPadding = 0.5f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        InvokeRepeating(nameof(SpawnPipe), initialSpawnDelay, spawnRate);
    }

    void SpawnPipe()
    {
        if (GameManager.IsGameOver)
            return;

        float y = Random.Range(-heightOffset, heightOffset);
        Instantiate(pipePrefab, new Vector3(GetSpawnX(), y, 0), Quaternion.identity);
        Debug.Log("Spawning pipe");
    }

    float GetSpawnX()
    {
        if (mainCamera == null || pipePrefab == null)
            return transform.position.x;

        float cameraRightEdge = mainCamera.transform.position.x
            + mainCamera.orthographicSize * mainCamera.aspect;

        return cameraRightEdge + GetPrefabHalfWidth() + spawnPadding;
    }

    float GetPrefabHalfWidth()
    {
        Renderer[] renderers = pipePrefab.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.extents.x;
    }
}
