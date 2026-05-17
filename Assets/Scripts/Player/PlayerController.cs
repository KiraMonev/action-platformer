using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(GroundDetector))]
public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public float moveSpeed = 5f;

    [Header("Input")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction attackAction;

    [Header("Jump")]
    public float jumpForce = 12f;

    private PlayerAnimations _animations;
    private GroundDetector _groundDetector;
    private float moveInput;

    private void Start()
    {
        _animations = GetComponent<PlayerAnimations>();
        _groundDetector = GetComponent<GroundDetector>();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        attackAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<float>();

        HandleFlip();
        HandleJump();
        HandleAttack();
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
        if (jumpAction.WasPressedThisFrame() && _groundDetector.IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void HandleAttack()
    {
        if (attackAction.WasPressedThisFrame())
        {
            if (_animations != null)
            {
                _animations.PlayAttack();
            }
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