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

    [Header("Audio Settings")]
    [SerializeField] private float footstepInterval = 0.35f;
    private float _footstepTimer;
    private bool _wasGrounded;

    private PlayerAnimations _animations;
    private GroundDetector _groundDetector;
    private float moveInput;

    private void Start()
    {
        _animations = GetComponent<PlayerAnimations>();
        _groundDetector = GetComponent<GroundDetector>();
        _wasGrounded = _groundDetector.IsGrounded;
        EnsureCameraFollow();
    }

    private void EnsureCameraFollow()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<CameraController>() == null)
            {
                mainCam.gameObject.AddComponent<CameraController>();
            }
        }
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

        HandleLanding();
        HandleFootsteps();
        HandleFlip();
        HandleJump();
        HandleAttack();
        UpdateAnimations();
    }

    private void HandleLanding()
    {
        bool isGrounded = _groundDetector.IsGrounded;
        if (isGrounded && !_wasGrounded && rb.linearVelocity.y <= 0.1f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(SoundType.Land);
            }
        }
        _wasGrounded = isGrounded;
    }

    private void HandleFootsteps()
    {
        if (_groundDetector.IsGrounded && moveInput != 0f)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.Play(SoundType.Run);
                }
                _footstepTimer = footstepInterval;
            }
        }
        else
        {
            _footstepTimer = 0f; // Сразу проиграть шаг при начале бега
        }
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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(SoundType.Jump);
            }
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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(SoundType.SwordAttack);
            }

            // Легкая отдача/тряска экрана для сочности атаки
            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.Shake(0.12f, 0.15f);
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
            _animations.isGrounded = _groundDetector.IsGrounded;
            _animations.yVelocity = rb.linearVelocity.y;
        }
    }
}