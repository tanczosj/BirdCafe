using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform first;
        public Transform second;
        public float speed = 1f;
    }

    public ParallaxLayer ground;
    public ParallaxLayer city;
    public ParallaxLayer clouds;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (GameManager.IsGameOver)
            return;

        MoveLayer(ground);
        MoveLayer(city);
        MoveLayer(clouds);
    }

    void MoveLayer(ParallaxLayer layer)
    {
        if (layer == null || layer.first == null || layer.second == null)
            return;

        MovePiece(layer.first, layer.speed);
        MovePiece(layer.second, layer.speed);

        RecycleIfOffscreen(layer.first, layer.second);
        RecycleIfOffscreen(layer.second, layer.first);
    }

    void MovePiece(Transform piece, float speed)
    {
        piece.position += Vector3.left * speed * Time.deltaTime;
    }

    void RecycleIfOffscreen(Transform piece, Transform otherPiece)
    {
        SpriteRenderer pieceRenderer = piece.GetComponent<SpriteRenderer>();
        SpriteRenderer otherRenderer = otherPiece.GetComponent<SpriteRenderer>();

        if (pieceRenderer == null || otherRenderer == null || mainCamera == null)
            return;

        float cameraLeftEdge = mainCamera.transform.position.x
            - mainCamera.orthographicSize * mainCamera.aspect;

        float pieceRightEdge = pieceRenderer.bounds.max.x;

        if (pieceRightEdge < cameraLeftEdge)
        {
            float otherRightEdge = otherRenderer.bounds.max.x;
            float pieceHalfWidth = pieceRenderer.bounds.size.x / 2f;

            piece.position = new Vector3(
                otherRightEdge + pieceHalfWidth,
                piece.position.y,
                piece.position.z
            );
        }
    }
}