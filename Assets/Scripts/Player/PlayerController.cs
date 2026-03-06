using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public float moveSpeed = 5f;

    [Header("Input")]
    public InputAction moveAction;
    public InputAction jumpAction;

    [Header("Jump")]
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private PlayerAnimations _animations;
    private float moveInput;
    private bool isGrounded;

    private void Start()
    {
        _animations = GetComponent<PlayerAnimations>();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<float>();
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        HandleFlip();
        HandleJump();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }
    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
    private void HandleJump()
    {
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }
    private void HandleFlip()
    {
        if (moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }
    }
    private void UpdateAnimations()
    {
        if (_animations != null)
        {
            _animations.isMoving = moveInput != 0;
        }
    }
}