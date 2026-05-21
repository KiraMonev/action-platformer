using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SmallBee : MonoBehaviour, IDamageable
{
    private enum State { Patrol, Charge, AttackPrep, Dashing, Cooldown, Hit, Dead }

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Movement (Patrol)")]
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float patrolRange = 4.0f;
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private float floatAmplitude = 0.4f;

    [Header("Detection")]
    [SerializeField] private float detectionWidth = 8.0f;
    [SerializeField] private float detectionHeight = 2.5f;

    [Header("Attack (Dash)")]
    [SerializeField] private float chargeSpeed = 4.5f;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackPrepDuration = 0.6f;
    [SerializeField] private float dashSpeed = 12.0f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float cooldownDuration = 1.5f;
    [SerializeField] private int contactDamage = 1;

    [Header("Physics & Ground Check")]
    [SerializeField] private LayerMask groundLayer;

    private State _currentState = State.Patrol;
    private int _facingDir = -1; // -1 = Left, 1 = Right
    private Vector2 _spawnPos;
    private float _stateTimer;
    private Transform _playerTransform;

    private Rigidbody2D _rb;
    private Animator _animator;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private Coroutine _hitCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _spawnPos = transform.position;
        currentHealth = maxHealth;

        // Default facing direction based on scale
        _facingDir = transform.localScale.x < 0 ? 1 : -1;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        // Configure Rigidbody2D for zero gravity flight
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void FixedUpdate()
    {
        // Don't execute behavior if dead
        if (_currentState == State.Dead) return;

        // Clean-up if bee falls too low
        if (transform.position.y < -25f)
        {
            Destroy(gameObject);
            return;
        }

        // Float wave Y height target (only relevant for Patrol and Cooldown/Prep)
        float targetY = _spawnPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        switch (_currentState)
        {
            case State.Patrol:
                ExecutePatrol(targetY);
                break;

            case State.Charge:
                ExecuteCharge();
                break;

            case State.AttackPrep:
                ExecuteAttackPrep();
                break;

            case State.Dashing:
                ExecuteDashing();
                break;

            case State.Cooldown:
                ExecuteCooldown(targetY);
                break;

            case State.Hit:
                // Smoothly damp the knockback velocity
                _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 12f);
                break;
        }
    }

    private void ExecutePatrol(float targetY)
    {
        // Horizontal patrol movement
        float targetVx = _facingDir * patrolSpeed;
        float targetVy = (targetY - transform.position.y) * 4f; // Smooth floating towards target Y
        _rb.linearVelocity = new Vector2(targetVx, targetVy);

        // Turn back if exceeded patrol range from spawn point
        float distFromSpawn = transform.position.x - _spawnPos.x;
        if (Mathf.Abs(distFromSpawn) >= patrolRange)
        {
            if (Mathf.Sign(distFromSpawn) == Mathf.Sign(_facingDir))
            {
                Flip();
            }
        }

        // Raycast forward to avoid walls
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, new Vector2(_facingDir, 0f), 0.8f, groundLayer);
        if (wallHit.collider != null)
        {
            Flip();
        }

        DetectPlayer();
    }

    private void DetectPlayer()
    {
        if (_playerTransform == null) return;

        // Target the player's chest (approx 1.2 units above pivot/feet)
        Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);
        Vector2 diff = targetPos - (Vector2)transform.position;
        if (Mathf.Abs(diff.x) <= detectionWidth / 2f && Mathf.Abs(diff.y) <= detectionHeight / 2f)
        {
            _currentState = State.Charge;
        }
    }

    private void ExecuteCharge()
    {
        if (_playerTransform == null)
        {
            _currentState = State.Patrol;
            return;
        }

        // Target the player's chest (approx 1.2 units above pivot/feet)
        Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);
        Vector2 diff = targetPos - (Vector2)transform.position;
        float xDist = Mathf.Abs(diff.x);
        float yDist = Mathf.Abs(diff.y);

        // If player gets out of detection bounds (with hysteresis/buffer), return to patrol
        if (xDist > detectionWidth / 2f * 1.15f || yDist > detectionHeight / 2f * 1.15f)
        {
            _currentState = State.Patrol;
            return;
        }

        // Face player horizontally
        float dirToPlayer = Mathf.Sign(diff.x);
        if (dirToPlayer != _facingDir)
        {
            Flip();
        }

        // Fly towards player horizontally and match vertical level smoothly
        float targetVx = _facingDir * chargeSpeed;
        float targetVy = diff.y * 4f; // match player chest height
        _rb.linearVelocity = new Vector2(targetVx, targetVy);

        // If player is close enough, stop and prepare the strike
        if (xDist <= attackRange)
        {
            StartAttackPrep();
        }
    }

    private void StartAttackPrep()
    {
        _currentState = State.AttackPrep;
        _stateTimer = attackPrepDuration;
        _rb.linearVelocity = Vector2.zero;

        // Ensure facing the player
        if (_playerTransform != null)
        {
            float dirToPlayer = Mathf.Sign(_playerTransform.position.x - transform.position.x);
            if (dirToPlayer != _facingDir)
            {
                Flip();
            }
        }

        if (_animator != null)
        {
            _animator.SetTrigger("attack");
        }
    }

    private void ExecuteAttackPrep()
    {
        _rb.linearVelocity = Vector2.zero; // menace/tension still pause
        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        _currentState = State.Dashing;
        _stateTimer = dashDuration;
    }

    private void ExecuteDashing()
    {
        // Strict horizontal dash in the facing direction
        _rb.linearVelocity = new Vector2(_facingDir * dashSpeed, 0f);

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            EnterCooldown();
        }
    }

    private void EnterCooldown()
    {
        _currentState = State.Cooldown;
        _stateTimer = cooldownDuration;
        _rb.linearVelocity = Vector2.zero;
    }

    private void ExecuteCooldown(float targetY)
    {
        // Float on Y axis while standing still horizontally
        float targetVy = (targetY - transform.position.y) * 4f;
        _rb.linearVelocity = new Vector2(0f, targetVy);

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            // After cooldown, check if we should charge player again
            if (_playerTransform != null)
            {
                Vector2 diff = _playerTransform.position - transform.position;
                if (Mathf.Abs(diff.x) <= detectionWidth / 2f && Mathf.Abs(diff.y) <= detectionHeight / 2f)
                {
                    _currentState = State.Charge;
                }
                else
                {
                    _currentState = State.Patrol;
                }
            }
            else
            {
                _currentState = State.Patrol;
            }
        }
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
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (_currentState == State.Dead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (_currentState == State.Dashing)
            {
                if (collision.gameObject.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    playerDamageable.TakeDamage(contactDamage);
                }

                // Immediately stop and bounce slightly on hit
                _rb.linearVelocity = Vector2.zero;
                _rb.AddForce(new Vector2(-_facingDir * 2f, 1f), ForceMode2D.Impulse);
                EnterCooldown();
            }
        }
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (_currentState == State.Dashing)
            {
                EnterCooldown();
            }
            else if (_currentState == State.Patrol)
            {
                Flip();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (_currentState == State.Dead) return;

        currentHealth -= damage;
        _rb.linearVelocity = Vector2.zero;

        if (_animator != null)
        {
            _animator.SetTrigger("hit");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Knockback push away from player (similar to the boar's knockback)
            if (_playerTransform != null)
            {
                float pushDir = Mathf.Sign(transform.position.x - _playerTransform.position.x);
                _rb.AddForce(new Vector2(pushDir * 5.0f, 2.5f), ForceMode2D.Impulse);
            }

            if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
            _hitCoroutine = StartCoroutine(HitRoutine());
        }
    }

    private IEnumerator HitRoutine()
    {
        _currentState = State.Hit;
        yield return new WaitForSeconds(0.4f);

        if (currentHealth > 0)
        {
            // Go back to patrol/detection
            _currentState = State.Patrol;
        }
    }

    private void Die()
    {
        _currentState = State.Dead;

        if (_animator != null)
        {
            _animator.SetBool("isDead", true);
        }

        if (_collider != null) _collider.enabled = false;
        
        // Pop up and fall down dead
        _rb.gravityScale = 1.2f;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(new Vector2(-_facingDir * 2f, 3.5f), ForceMode2D.Impulse);

        Destroy(gameObject, 0.8f);
    }

    private void OnDrawGizmos()
    {
        // Soft transparent cyan for detection box (moves with the bee)
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionWidth, detectionHeight, 0f));

        // Soft yellow for patrol range (centered at spawn point)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
        Vector3 spawn = Application.isPlaying ? (Vector3)_spawnPos : transform.position;
        Vector3 leftBound = spawn - new Vector3(patrolRange, 0f, 0f);
        Vector3 rightBound = spawn + new Vector3(patrolRange, 0f, 0f);
        
        Gizmos.DrawLine(leftBound, rightBound);
        Gizmos.DrawLine(leftBound + Vector3.up * 0.15f, leftBound + Vector3.down * 0.15f);
        Gizmos.DrawLine(rightBound + Vector3.up * 0.15f, rightBound + Vector3.down * 0.15f);
        Gizmos.DrawWireSphere(spawn, 0.08f);

        // Soft orange for attack range trigger (moves with the bee)
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawLine(transform.position - new Vector3(attackRange, 0f, 0f), transform.position + new Vector3(attackRange, 0f, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        // Bright solid cyan for selected detection box (moves with the bee)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionWidth, detectionHeight, 0f));

        // Bright yellow for selected patrol range (centered at spawn point)
        Gizmos.color = Color.yellow;
        Vector3 spawn = Application.isPlaying ? (Vector3)_spawnPos : transform.position;
        Vector3 leftBound = spawn - new Vector3(patrolRange, 0f, 0f);
        Vector3 rightBound = spawn + new Vector3(patrolRange, 0f, 0f);

        Gizmos.DrawLine(leftBound, rightBound);
        Gizmos.DrawLine(leftBound + Vector3.up * 0.2f, leftBound + Vector3.down * 0.2f);
        Gizmos.DrawLine(rightBound + Vector3.up * 0.2f, rightBound + Vector3.down * 0.2f);
        Gizmos.DrawWireSphere(spawn, 0.1f);

        // Solid orange for attack range trigger (moves with the bee)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos - new Vector3(attackRange, 0f, 0f), pos + new Vector3(attackRange, 0f, 0f));
        Gizmos.DrawLine(pos - new Vector3(attackRange, 0f, 0f) + Vector3.up * 0.15f, pos - new Vector3(attackRange, 0f, 0f) + Vector3.down * 0.15f);
        Gizmos.DrawLine(pos + new Vector3(attackRange, 0f, 0f) + Vector3.up * 0.15f, pos + new Vector3(attackRange, 0f, 0f) + Vector3.down * 0.15f);
    }
}
