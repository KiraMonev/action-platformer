using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public float moveSpeed = 5f;

    [Header("Input")]
    public InputAction moveAction;

    private float moveInput;

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }


    void Update()
    {
        moveInput = moveAction.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
}