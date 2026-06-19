using UnityEngine;
using UnityEngine.InputSystem;

public class Bird : MonoBehaviour
{
    public float jumpForce = 5f;
    public float maxUpAngle = 30f;
    public float maxDownAngle = -90f;
    public float rotationSpeed = 5f;

    private Rigidbody2D rb;
    private InputAction jumpAction;
    private bool isDead;
    private bool isFrozen;

    void Awake()
    {
        // Assign rb here so FreezeBird() can safely call it before Start()
        rb = GetComponent<Rigidbody2D>();

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Mouse>/leftButton");
    }

    void OnEnable() { jumpAction.Enable(); }
    void OnDisable() { jumpAction.Disable(); }

    void Start()
    {
        // GameManager freezes the bird on Awake, so re-apply here to be safe
        if (GameManager.gameFrozen)
            FreezeBird();
    }

    void Update()
    {
        if (isDead || isFrozen || GameManager.gameFrozen)
            return;

        if (jumpAction.WasPressedThisFrame())
            rb.linearVelocity = Vector2.up * jumpForce;

        float tilt = Mathf.Clamp(rb.linearVelocity.y * 10f, maxDownAngle, maxUpAngle);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, 0, tilt),
            rotationSpeed * Time.deltaTime
        );
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isFrozen || GameManager.IsGameOver)
            return;

        isDead = true;
        FreezeBird();

        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }

    public void FreezeBird()
    {
        isFrozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        jumpAction.Disable();
    }

    public void UnfreezeBird()
    {
        isFrozen = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = true;
        jumpAction.Enable();
    }
}