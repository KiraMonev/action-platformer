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
    public InputAction fireballAction;

    [Header("Jump")]
    public float jumpForce = 12f;

    [Header("Fireball Attack")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;

    [Header("Audio Settings")]
    [SerializeField] private float footstepInterval = 0.35f;
    private float _footstepTimer;
    private bool _wasGrounded;

    private PlayerAnimations _animations;
    private GroundDetector _groundDetector;
    private float moveInput;
    private bool _isCasting;
    private bool _isGroundedCast;
    private float _knockbackTimer;
    private bool _isDead;

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
        fireballAction.Enable();

        if (TryGetComponent<Player>(out var player))
        {
            player.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        attackAction.Disable();
        fireballAction.Disable();

        if (TryGetComponent<Player>(out var player))
        {
            player.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        _isDead = true;
        moveInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        OnDisable();
    }

    void Update()
    {
        if (_isDead) return;

        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
        }

        if (_isCasting && _isGroundedCast)
        {
            moveInput = 0f;
        }
        else if (_knockbackTimer <= 0f)
        {
            moveInput = moveAction.ReadValue<float>();
        }

        HandleLanding();
        HandleFootsteps();
        HandleFlip();
        HandleJump();
        HandleAttack();
        HandleFireball();
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
        if (_groundDetector.IsGrounded && moveInput != 0f && _knockbackTimer <= 0f)
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
            _footstepTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_knockbackTimer > 0f) return;

        if (_isCasting && _isGroundedCast)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (_knockbackTimer > 0f || (_isCasting && _isGroundedCast)) return;

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
        if (_knockbackTimer > 0f || (_isCasting && _isGroundedCast)) return;

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

            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.Shake(0.12f, 0.15f);
            }

            Vector2 attackCenter = (Vector2)transform.position + new Vector2(Mathf.Sign(transform.localScale.x) * 1.1f, 0f);
            Vector2 attackSize = new Vector2(1.8f, 2.2f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackSize, 0f);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(1);
                }
            }
        }
    }

    private void HandleFlip()
    {
        if (_knockbackTimer > 0f) return;

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
            _animations.isMoving = moveInput != 0 && _knockbackTimer <= 0f;
            _animations.isGrounded = _groundDetector.IsGrounded;
            _animations.yVelocity = rb.linearVelocity.y;
        }
    }

    private void HandleFireball()
    {
        if (_knockbackTimer > 0f || _isCasting) return;

        if (fireballAction.WasPressedThisFrame())
        {
            _isCasting = true;
            _isGroundedCast = _groundDetector.IsGrounded;
            if (_animations != null)
            {
                _animations.PlayCast();
            }
        }
    }

    public void SpawnFireball()
    {
        if (_isDead) return;

        if (fireballPrefab == null || fireballSpawnPoint == null)
        {
            Debug.LogWarning("[PlayerController] fireballPrefab or fireballSpawnPoint is not assigned!");
            return;
        }

        GameObject fireballObj = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
        Fireball fireball = fireballObj.GetComponent<Fireball>();
        if (fireball != null)
        {
            Vector2 launchDir = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
            fireball.Launch(launchDir);
        }

        CameraController cam = Camera.main?.GetComponent<CameraController>();
        if (cam != null)
        {
            cam.Shake(0.08f, 0.12f);
        }
    }

    public void EndCast()
    {
        _isCasting = false;
    }

    public void ApplyKnockback(Vector2 force, float duration = 0.25f)
    {
        if (_isDead) return;

        _knockbackTimer = duration;
        _isCasting = false;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}
