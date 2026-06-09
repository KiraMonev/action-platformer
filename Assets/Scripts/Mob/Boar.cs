using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Boar : MonoBehaviour, IDamageable
{
    private enum State { Patrol, Charge, AttackPrep, Attacking, Cooldown, Hit, Dead }

    [Header("Health")]
    [SerializeField] private int health = 2;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chargeSpeed = 7.0f;
    [SerializeField] private float groundCheckDist = 1.0f;
    [SerializeField] private float wallCheckDist = 0.8f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Detection & Attack")]
    [SerializeField] private float detectionDistance = 7.0f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackPrepDuration = 0.6f; // Четкая пауза 0.6 секунд!

    [Header("Cooldown")]
    [SerializeField] private float cooldownDuration = 1.8f;

    private State _currentState = State.Patrol;
    private int _facingDir = -1; // Кабан по умолчанию смотрит влево (-1)
    private Rigidbody2D _rb;
    private Animator _animator;
    private Collider2D _collider;
    private float _stateTimer;
    private Coroutine _hitCoroutine;
    private Transform _playerTransform;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        _facingDir = transform.localScale.x < 0 ? 1 : -1;
        var player = GameObject.FindWithTag("Player");
        if (player != null) _playerTransform = player.transform;
        
        SetAnimation("Walk");
    }

    private void SetAnimation(string animName)
    {
        if (_animator != null)
        {
            _animator.Play(animName);
        }
    }

    private void FixedUpdate()
    {
        if (transform.position.y < -25f)
        {
            Destroy(gameObject);
            return;
        }

        if (_currentState == State.Dead || _currentState == State.Hit) return;

        if (_currentState == State.Cooldown)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer <= 0f)
            {
                _currentState = State.Patrol;
                SetAnimation("Walk");
            }
            return;
        }

        if (_currentState == State.AttackPrep)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer <= 0f)
            {
                ExecuteAttack();
            }
            return;
        }

        CheckEnvironment();

        if (_currentState == State.Attacking)
        {
            _rb.linearVelocity = new Vector2(_facingDir * chargeSpeed, _rb.linearVelocity.y);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer <= 0f)
            {
                EnterCooldown();
            }
            return;
        }

        if (_currentState == State.Patrol)
        {
            _rb.linearVelocity = new Vector2(_facingDir * patrolSpeed, _rb.linearVelocity.y);
            DetectPlayer();
        }
        else if (_currentState == State.Charge)
        {
            _rb.linearVelocity = new Vector2(_facingDir * chargeSpeed, _rb.linearVelocity.y);
            CheckAttackRange();
        }
    }

    private void CheckEnvironment()
    {
        Vector2 pos = transform.position;
        Vector2 frontBottom = pos + new Vector2(_facingDir * 0.7f, -0.5f);

        RaycastHit2D groundHit = Physics2D.Raycast(frontBottom, Vector2.down, groundCheckDist, groundLayer);
        RaycastHit2D wallHit = Physics2D.Raycast(pos, new Vector2(_facingDir, 0), wallCheckDist, groundLayer);

        if (groundHit.collider == null || wallHit.collider != null)
        {
            if (_currentState == State.Charge || _currentState == State.Attacking)
            {
                EnterCooldown();
            }
            else
            {
                Flip();
            }
        }
    }

    private void DetectPlayer()
    {
        if (_playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist <= detectionDistance)
        {
            // Проверяем, что игрок находится спереди (по направлению взгляда кабана)
            float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
            if (dirToPlayer == Mathf.Sign(_facingDir))
            {
                _currentState = State.Charge;
                SetAnimation("Run");
            }
        }
    }

    private void CheckAttackRange()
    {
        if (_playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist <= attackRange)
        {
            // Тормозим, если игрок спереди ИЛИ если мы оказались слишком близко (дистанция < 0.6)
            float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
            if (dirToPlayer == Mathf.Sign(_facingDir) || dist < 0.6f)
            {
                StartAttackPrep();
            }
        }
    }

    private void StartAttackPrep()
    {
        _currentState = State.AttackPrep;
        _stateTimer = attackPrepDuration; // Гарантированная пауза
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        SetAnimation("Idle"); // Кабан замирает перед выпадом
    }

    private void ExecuteAttack()
    {
        _currentState = State.Attacking;
        _stateTimer = 0.4f; // Длительность финального выпада
        SetAnimation("Run");
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
        HandlePlayerCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandlePlayerCollision(collision);
    }

    private void HandlePlayerCollision(Collision2D collision)
    {
        if (_currentState != State.Attacking) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var playerDamageable))
            {
                playerDamageable.TakeDamage(1);
            }

            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(new Vector2(-_facingDir * 3.5f, 2.0f), ForceMode2D.Impulse);

            EnterCooldown();
        }
    }

    private void EnterCooldown()
    {
        _currentState = State.Cooldown;
        _stateTimer = cooldownDuration + Random.Range(-0.2f, 0.2f);
        SetAnimation("Idle");
    }

    public void TakeDamage(int damage)
    {
        if (_currentState == State.Dead) return;

        health -= damage;
        _rb.linearVelocity = Vector2.zero;

        float bloodPushDir = _playerTransform != null ? Mathf.Sign(transform.position.x - _playerTransform.position.x) : -_facingDir;
        Vector2 bloodDir = new Vector2(bloodPushDir, 0.4f).normalized;
        if (FXManager.Instance != null)
        {
            FXManager.Instance.PlayHitBlood(transform.position, bloodDir);
        }

        if (health <= 0)
        {
            _currentState = State.Dead;
            SetAnimation("Hit-Vanish");
            if (_collider != null) _collider.enabled = false;
            if (_rb != null) _rb.simulated = false;
            Destroy(gameObject, 0.7f);
        }
        else
        {
            if (_playerTransform != null)
            {
                float pushDir = Mathf.Sign(transform.position.x - _playerTransform.position.x);
                _rb.AddForce(new Vector2(pushDir * 4.5f, 3.0f), ForceMode2D.Impulse);
            }
            
            if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
            _hitCoroutine = StartCoroutine(HitRoutine());
        }
    }

    private IEnumerator HitRoutine()
    {
        _currentState = State.Hit;
        SetAnimation("Hit-Vanish");
        yield return new WaitForSeconds(0.3f);
        
        if (health > 0)
        {
            _currentState = State.Charge;
            SetAnimation("Run");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Vector2 pos = transform.position;
        int dir = transform.localScale.x < 0 ? 1 : -1;
        Vector2 frontBottom = pos + new Vector2(dir * 0.7f, -0.5f);
        Gizmos.DrawLine(frontBottom, frontBottom + Vector2.down * groundCheckDist);
        Gizmos.DrawLine(pos, pos + new Vector2(dir * wallCheckDist, 0));
    }
}
