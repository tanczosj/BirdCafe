using UnityEngine;

public class CartoonBob2D : MonoBehaviour
{
    [SerializeField] private float bobHeight = 8f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float rotationAmount = 3f;
    [SerializeField] private float rotationSpeed = 1.5f;
    [SerializeField] private bool useLocalPosition = true;

    private Vector3 startPos;
    private Quaternion startRot;
    private float seed;

    private void Awake()
    {
        startPos = useLocalPosition ? transform.localPosition : transform.position;
        startRot = transform.localRotation;
        seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time + seed;

        float y = Mathf.Sin(t * bobSpeed) * bobHeight;
        float rotZ = Mathf.Sin(t * rotationSpeed) * rotationAmount;

        if (useLocalPosition)
            transform.localPosition = startPos + new Vector3(0f, y, 0f);
        else
            transform.position = startPos + new Vector3(0f, y, 0f);

        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, rotZ);
    }
}