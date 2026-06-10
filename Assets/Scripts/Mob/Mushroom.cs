using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Mushroom : MonoBehaviour, IDamageable
{
    private enum State { Idle, Patrol, Chase, AttackPrep, Attacking, Cooldown, Hit, Dead }

    [Header("Health")]
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int currentHealth;

    [Header("Patrol")]
    [SerializeField] private float patrolWidth = 6.0f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float idleDurationMin = 1.0f;
    [SerializeField] private float idleDurationMax = 3.0f;

    [Header("Player Detection")]
    [SerializeField] private float detectionWidth = 8.0f;
    [SerializeField] private float detectionHeight = 3.0f;
    [SerializeField] private float leashMultiplier = 1.5f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.0f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackPrepDuration = 0.4f;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float cooldownDuration = 1.0f;
    [SerializeField] private int contactDamage = 1;

    [Header("Physics")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float edgeCheckDistance = 1.5f;

    private State _currentState = State.Patrol;
    private int _facingDir = -1;
    private Vector2 _spawnPos;
    private float _stateTimer;
    private Transform _playerTransform;

    public string CurrentState => _currentState.ToString();
    public Vector2 SpawnPos => _spawnPos;
    public int FacingDir => _facingDir;
    public bool DebugIsWallAhead => IsWallAhead();
    public bool DebugIsGroundAhead => IsGroundAhead();

    private Rigidbody2D _rb;
    private Animator _animator;
    private Collider2D _collider;
    private BoxCollider2D _boxCollider;
    private SpriteRenderer _spriteRenderer;
    private Coroutine _hitCoroutine;
    private bool _hasDealtDamageThisAttack;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void SetAnimation(string animName)
    {
        if (_animator != null)
        {
            _animator.Play(animName);
        }
    }

    private void SetState(State newState)
    {
        _currentState = newState;

        if (_rb != null)
        {
            if (newState == State.Patrol || newState == State.Chase || newState == State.Hit || newState == State.Dead)
            {
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                _rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
        }

        switch (newState)
        {
            case State.Idle:
                SetAnimation("Idle");
                break;
            case State.Patrol:
                SetAnimation("Walk");
                break;
            case State.Chase:
                SetAnimation("Run");
                break;
            case State.AttackPrep:
                SetAnimation("Idle");
                break;
            case State.Attacking:
                SetAnimation("Attack");
                _hasDealtDamageThisAttack = false;
                break;
            case State.Cooldown:
                SetAnimation("Idle");
                break;
            case State.Hit:
                SetAnimation("Hit-Vanish");
                break;
            case State.Dead:
                SetAnimation("Die");
                break;
        }
    }

    private void Start()
    {
        _spawnPos = transform.position;
        currentHealth = maxHealth;

        _facingDir = transform.localScale.x < 0 ? 1 : -1;

        // Ignore collisions with other mushrooms
        Mushroom[] allMushrooms = FindObjectsByType<Mushroom>(FindObjectsSortMode.None);
        foreach (var m in allMushrooms)
        {
            if (m != this && m.TryGetComponent<Collider2D>(out var otherCollider))
            {
                Physics2D.IgnoreCollision(_collider, otherCollider, true);
            }
        }

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        _rb.gravityScale = 2.5f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.bodyType = RigidbodyType2D.Dynamic;

        _stateTimer = Random.Range(idleDurationMin, idleDurationMax);
        SetAnimation("Walk");
    }

    private void FixedUpdate()
    {
        if (_currentState == State.Dead) return;

        if (transform.position.y < -25f)
        {
            Destroy(gameObject);
            return;
        }

        switch (_currentState)
        {
            case State.Idle:
                ExecuteIdle();
                break;
            case State.Patrol:
                ExecutePatrol();
                break;
            case State.Chase:
                ExecuteChase();
                break;
            case State.AttackPrep:
                ExecuteAttackPrep();
                break;
            case State.Attacking:
                ExecuteAttacking();
                break;
            case State.Cooldown:
                ExecuteCooldown();
                break;
            case State.Hit:
                _rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(_rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 15f),
                    _rb.linearVelocity.y);
                break;
        }
    }

    private void ExecuteIdle()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            SetState(State.Patrol);
            // Randomly choose direction
            if (Random.value > 0.5f) Flip();
        }

        DetectPlayer();
    }

    private void ExecutePatrol()
    {
        // Check for wall or edge
        if (IsWallAhead() || !IsGroundAhead())
        {
            Flip();
        }

        _rb.linearVelocity = new Vector2(_facingDir * patrolSpeed, _rb.linearVelocity.y);

        // Turn back if exceeded patrol range
        float distFromSpawn = transform.position.x - _spawnPos.x;
        float halfWidth = patrolWidth / 2f;
        if (Mathf.Abs(distFromSpawn) >= halfWidth)
        {
            if (Mathf.Sign(distFromSpawn) == Mathf.Sign(_facingDir))
            {
                Flip();
                SetState(State.Idle);
                _stateTimer = Random.Range(idleDurationMin, idleDurationMax);
                return;
            }
        }

        DetectPlayer();
    }

    private void ExecuteChase()
    {
        if (_playerTransform == null)
        {
            SetState(State.Patrol);
            return;
        }

        // Return to patrol if outside leash zone
        if (!IsInLeashZone(transform.position))
        {
            SetState(State.Patrol);
            return;
        }

        // Return to patrol if player too far
        if (!IsPlayerInDetectionRange())
        {
            SetState(State.Patrol);
            return;
        }

        float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
        if (dirToPlayer != _facingDir)
        {
            Flip();
        }

        // Check for wall or edge
        if (IsWallAhead() || !IsGroundAhead())
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
        else
        {
            _rb.linearVelocity = new Vector2(_facingDir * chaseSpeed, _rb.linearVelocity.y);
        }

        // Close enough to attack?
        float xDist = Mathf.Abs(_playerTransform.position.x - transform.position.x);
        if (xDist <= attackRange)
        {
            StartAttackPrep();
        }
    }

    private void StartAttackPrep()
    {
        SetState(State.AttackPrep);
        _stateTimer = attackPrepDuration;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        if (_playerTransform != null)
        {
            float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
            if (dirToPlayer != _facingDir)
            {
                Flip();
            }
        }
    }

    private void ExecuteAttackPrep()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            SetState(State.Attacking);
            _stateTimer = attackDuration;
        }
    }

    private void ExecuteAttacking()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        if (!_hasDealtDamageThisAttack && _playerTransform != null)
        {
            float xDist = Mathf.Abs(_playerTransform.position.x - transform.position.x);
            float yDist = Mathf.Abs(_playerTransform.position.y - transform.position.y);

            if (xDist <= attackRange && yDist <= detectionHeight / 2f)
            {
                float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
                if (dirToPlayer == Mathf.Sign(_facingDir))
                {
                    if (_playerTransform.TryGetComponent<IDamageable>(out var playerDamageable))
                    {
                        playerDamageable.TakeDamage(contactDamage);
                        _hasDealtDamageThisAttack = true;
                        Debug.Log($"[Mushroom] Dealt {contactDamage} damage via attack range detection.");
                    }
                }
            }
        }

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            EnterCooldown();
        }
    }

    private void EnterCooldown()
    {
        SetState(State.Cooldown);
        _stateTimer = cooldownDuration;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    private void ExecuteCooldown()
    {
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            if (_playerTransform != null && IsPlayerInDetectionRange() && IsInLeashZone(transform.position))
            {
                SetState(State.Chase);
            }
            else
            {
                SetState(State.Patrol);
            }
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        if (_playerTransform == null) return false;
        Vector2 diff = (Vector2)_playerTransform.position - (Vector2)transform.position;
        return Mathf.Abs(diff.x) <= detectionWidth / 2f && Mathf.Abs(diff.y) <= detectionHeight / 2f;
    }

    private bool IsInLeashZone(Vector2 position)
    {
        Vector2 diff = position - _spawnPos;
        float leashWidth = detectionWidth * leashMultiplier;
        float leashHeight = detectionHeight * leashMultiplier;
        return Mathf.Abs(diff.x) <= leashWidth / 2f && Mathf.Abs(diff.y) <= leashHeight / 2f;
    }

    private void DetectPlayer()
    {
        if (IsPlayerInDetectionRange())
        {
            SetState(State.Chase);
        }
    }

    private bool IsWallAhead()
    {
        // Primary check: use physics contacts to detect walls.
        // This catches ground-level ledges/steps that raycasts can miss.
        if (_collider != null)
        {
            ContactPoint2D[] contacts = new ContactPoint2D[8];
            int count = _collider.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                // A horizontal contact normal opposing our facing direction means wall,
                // but ONLY if the contact point is AHEAD of our center (in facing dir).
                float normalX = contacts[i].normal.x;
                if (Mathf.Abs(normalX) > 0.7f && Mathf.Sign(normalX) != Mathf.Sign(_facingDir))
                {
                    float contactRelX = contacts[i].point.x - transform.position.x;
                    if (Mathf.Sign(contactRelX) == Mathf.Sign(_facingDir))
                    {
                        return true;
                    }
                }
            }
        }

        // Secondary check: raycast at mid-height for pre-detecting walls
        // before we physically touch them.
        if (_boxCollider == null) return false;

        float halfWidth = (_boxCollider.size.x * Mathf.Abs(transform.localScale.x)) / 2f;
        float worldOffsetY = _boxCollider.offset.y * transform.localScale.y;

        float midY = transform.position.y + worldOffsetY;
        Vector2 midOrigin = new Vector2(transform.position.x, midY);
        float castDist = halfWidth + wallCheckDistance;

        RaycastHit2D midHit = Physics2D.Raycast(midOrigin, new Vector2(_facingDir, 0f), castDist, groundLayer);

        Debug.DrawRay(midOrigin, new Vector2(_facingDir * castDist, 0f), Color.red);

        return midHit.collider != null;
    }

    private bool IsGroundAhead()
    {
        if (_boxCollider == null) return false;

        float halfWidth = (_boxCollider.size.x * Mathf.Abs(transform.localScale.x)) / 2f;
        float halfHeight = (_boxCollider.size.y * Mathf.Abs(transform.localScale.y)) / 2f;
        float worldOffsetY = _boxCollider.offset.y * transform.localScale.y;

        // Start horizontally ahead of the front edge, but vertically at the
        // collider center — this ensures the ray starts ABOVE the ground even
        // when the mushroom is slightly buried, and casts far enough down.
        float startX = transform.position.x + (_facingDir * (halfWidth + 0.1f));
        float startY = transform.position.y + worldOffsetY;

        Vector2 origin = new Vector2(startX, startY);
        float castDist = halfHeight + edgeCheckDistance;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, castDist, groundLayer);

        Debug.DrawRay(origin, Vector2.down * castDist, Color.green);

        return hit.collider != null;
    }

    private void Flip()
    {
        _facingDir = -_facingDir;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (-_facingDir);
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<Mushroom>(out _))
        {
            Physics2D.IgnoreCollision(_collider, collision.collider, true);
            return;
        }
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<Mushroom>(out _))
        {
            Physics2D.IgnoreCollision(_collider, collision.collider, true);
            return;
        }
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (_currentState == State.Dead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (_currentState == State.Attacking)
            {
                // Deal damage once, but do NOT force EnterCooldown here.
                // Let the attack timer in ExecuteAttacking() finish naturally
                // so the animation plays to completion without a second trigger.
                if (!_hasDealtDamageThisAttack)
                {
                    if (collision.gameObject.TryGetComponent<IDamageable>(out var playerDamageable))
                    {
                        playerDamageable.TakeDamage(contactDamage);
                        _hasDealtDamageThisAttack = true;
                        Debug.Log($"[Mushroom] Dealt {contactDamage} damage via collision.");
                    }
                }
            }
            else if (_currentState == State.Patrol)
            {
                float dirToPlayer = Mathf.Sign(collision.transform.position.x - transform.position.x);
                if (dirToPlayer == Mathf.Sign(_facingDir))
                {
                    Flip();
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (_currentState == State.Dead) return;

        currentHealth -= damage;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        float bloodPushDir = _playerTransform != null ? Mathf.Sign(transform.position.x - _playerTransform.position.x) : -_facingDir;
        Vector2 bloodDir = new Vector2(bloodPushDir, 0.4f).normalized;
        if (FXManager.Instance != null)
        {
            Vector2 spawnPos = _collider != null ? (Vector2)_collider.bounds.center : (Vector2)transform.position;
            FXManager.Instance.PlayHitBlood(spawnPos, bloodDir);
        }

        // Animation is handled by SetState(State.Hit) inside HitRoutine,
        // which calls _animator.Play("Hit-Vanish"). No trigger needed.

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (_playerTransform != null)
            {
                float pushDir = Mathf.Sign(transform.position.x - _playerTransform.position.x);
                _rb.AddForce(new Vector2(pushDir * 4.0f, 2.0f), ForceMode2D.Impulse);
            }

            if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
            _hitCoroutine = StartCoroutine(HitRoutine());
        }
    }

    private IEnumerator HitRoutine()
    {
        SetState(State.Hit);
        yield return new WaitForSeconds(0.4f);

        if (currentHealth > 0)
        {
            if (_playerTransform != null && IsPlayerInDetectionRange() && IsInLeashZone(transform.position))
            {
                SetState(State.Chase);
            }
            else
            {
                SetState(State.Patrol);
            }
        }
    }

    private void Die()
    {
        SetState(State.Dead);

        if (_animator != null)
        {
            _animator.SetBool("isDead", true);
        }

        if (_collider != null) _collider.enabled = false;

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(new Vector2(-_facingDir * 1.5f, 3.0f), ForceMode2D.Impulse);

        Destroy(gameObject, 1.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)_spawnPos : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(patrolWidth, 1f, 0f));

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
        Gizmos.DrawWireCube(center, new Vector3(detectionWidth * leashMultiplier, detectionHeight * leashMultiplier, 0f));

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos - new Vector3(attackRange, 0f, 0f), pos + new Vector3(attackRange, 0f, 0f));

        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.4f);
        Gizmos.DrawWireCube(pos, new Vector3(detectionWidth, detectionHeight, 0f));
    }
}
