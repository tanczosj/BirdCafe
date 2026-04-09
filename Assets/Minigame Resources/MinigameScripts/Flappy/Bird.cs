using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Bird : MonoBehaviour
{
    public float jumpForce = 5f;
    private Rigidbody2D rb;

    public float maxUpAngle = 30f;
    public float maxDownAngle = -90f;
    public float rotationSpeed = 5f;
    private MinigameSceneController _sceneController;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _sceneController = FindAnyObjectByType<MinigameSceneController>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            rb.linearVelocity = Vector2.up * jumpForce;
        }
        float tilt = Mathf.Clamp(rb.linearVelocity.y * 10f, maxDownAngle, maxUpAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, tilt), rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_sceneController != null)
            _sceneController.ReportFailure(0, "You crashed.");
    }
}