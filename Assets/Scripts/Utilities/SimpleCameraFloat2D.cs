using UnityEngine;

public class SimpleCameraFloat2D : MonoBehaviour
{
    [SerializeField] private Vector3 moveAmount = new Vector3(8f, 5f, 0f);
    [SerializeField] private float moveSpeed = 0.6f;

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void LateUpdate()
    {
        float x = Mathf.Sin(Time.time * moveSpeed) * moveAmount.x;
        float y = Mathf.Cos(Time.time * moveSpeed * 0.8f) * moveAmount.y;

        transform.position = startPos + new Vector3(x, y, 0f);
    }
}