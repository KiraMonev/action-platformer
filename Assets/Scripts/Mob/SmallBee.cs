using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SmallBee : MonoBehaviour, IDamageable
{
    private enum State { Patrol, Charge, AttackPrep, Dashing, Cooldown, Hit, Dead }

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Patrol Zone (Blue Rectangle)")]
    [SerializeField] private float patrolWidth = 8.0f;
    [SerializeField] private float patrolHeight = 2.5f;
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private float floatAmplitude = 0.4f;

    [Header("Player Detection Range (Purple Rectangle)")]
    [SerializeField] private float detectionWidth = 14.0f;
    [SerializeField] private float detectionHeight = 4.5f;
    [Tooltip("Multiplier for detection range to define the chase limit (yellow zone) before the bee returns.")]
    [SerializeField] private float leashMultiplier = 1.5f;

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

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleCheckDistance = 1.0f;
    [SerializeField] private float probeHeight = 1.0f;
    [SerializeField] private float probeDepth = 2.0f;
    [SerializeField] private float lookAheadDistance = 0.8f;
    [SerializeField] private float minAltitude = 0.8f;
    [SerializeField] private float climbSpeed = 5.0f;
    [SerializeField] private float fallSpeed = 3.0f;
    [SerializeField] private float upperRayOffset = 0.8f;

    private State _currentState = State.Patrol;
    private int _facingDir = -1; // -1 = Left, 1 = Right
    private Vector2 _spawnPos;
    private float _stateTimer;
    private Transform _playerTransform;
    private float _obstacleYOffset = 0f;

    public string CurrentState => _currentState.ToString();
    public Vector2 SpawnPos => _spawnPos;

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

        UpdateObstacleAvoidance();

        // Float wave Y height target (only relevant for Patrol and Cooldown/Prep)
        // Obstacle Y offset is added to the base height so the sinusoid pattern is preserved cleanly.
        float targetY = (_spawnPos.y + _obstacleYOffset) + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

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

    private void UpdateObstacleAvoidance()
    {
        if (_currentState != State.Patrol && _currentState != State.Charge && _currentState != State.Cooldown)
        {
            _obstacleYOffset = Mathf.MoveTowards(_obstacleYOffset, 0f, Time.fixedDeltaTime * fallSpeed);
            return;
        }

        float bodyRadius = 0.4f;
        if (_collider is CircleCollider2D circle)
        {
            bodyRadius = circle.radius * Mathf.Abs(transform.localScale.x);
        }

        Vector2 origin = (Vector2)transform.position + new Vector2(_facingDir * bodyRadius, 0f);
        Vector2 direction = new Vector2(_facingDir, 0f);

        // Center forward ray for wall detection
        RaycastHit2D centerHit = Physics2D.Raycast(origin, direction, obstacleCheckDistance, groundLayer);

        // Upper forward ray to check if we can fly over
        Vector2 upperOrigin = origin + Vector2.up * upperRayOffset;
        RaycastHit2D upperHit = Physics2D.Raycast(upperOrigin, direction, obstacleCheckDistance, groundLayer);

        bool obstacleAhead = centerHit.collider != null;
        bool canFlyOver = upperHit.collider == null;

        // Downward probes to find ground level below and ahead
        Vector2 probeOriginBelow = (Vector2)transform.position + Vector2.up * probeHeight;
        Vector2 probeOriginAhead = probeOriginBelow + new Vector2(_facingDir * lookAheadDistance, 0f);
        float rayLength = probeHeight + probeDepth;

        RaycastHit2D hitBelow = Physics2D.Raycast(probeOriginBelow, Vector2.down, rayLength, groundLayer);
        RaycastHit2D hitAhead = Physics2D.Raycast(probeOriginAhead, Vector2.down, rayLength, groundLayer);

        float groundBelowY = hitBelow.collider != null ? hitBelow.point.y : -Mathf.Infinity;
        float groundAheadY = hitAhead.collider != null ? hitAhead.point.y : -Mathf.Infinity;
        float maxGroundY = Mathf.Max(groundBelowY, groundAheadY);

        float desiredOffset = 0f;
        if (maxGroundY != -Mathf.Infinity)
        {
            if (_currentState == State.Charge)
            {
                // During charge, we target the player's chest height directly (no sinusoid)
                float playerTargetY = (_playerTransform != null) ? (_playerTransform.position.y + 1.2f) : transform.position.y;
                desiredOffset = Mathf.Max(0f, (maxGroundY + minAltitude) - playerTargetY);
            }
            else
            {
                // During patrol or cooldown, we target a sinusoid.
                // We want the lowest point of the sinusoid to clear the ground.
                desiredOffset = Mathf.Max(0f, (maxGroundY + minAltitude + floatAmplitude) - _spawnPos.y);
            }
        }

        // Handle wall flip: if there is an obstacle in front and we cannot fly over it
        if (_currentState == State.Patrol && obstacleAhead && !canFlyOver)
        {
            Flip();
            _obstacleYOffset = 0f;
            return;
        }
        else if (_currentState == State.Charge && obstacleAhead && !canFlyOver)
        {
            // Abort charge if blocked by a tall wall
            _currentState = State.Patrol;
            _obstacleYOffset = 0f;
            return;
        }

        // Smoothly interpolate current offset towards desired offset
        if (_obstacleYOffset < desiredOffset)
        {
            _obstacleYOffset = Mathf.MoveTowards(_obstacleYOffset, desiredOffset, Time.fixedDeltaTime * climbSpeed);
        }
        else
        {
            _obstacleYOffset = Mathf.MoveTowards(_obstacleYOffset, desiredOffset, Time.fixedDeltaTime * fallSpeed);
        }
    }

    private void ExecutePatrol(float targetY)
    {
        // Horizontal patrol movement
        float targetVx = _facingDir * patrolSpeed;
        float targetVy = (targetY - transform.position.y) * 4f; // Smooth floating towards target Y (offset built-in)
        _rb.linearVelocity = new Vector2(targetVx, targetVy);

        // Turn back if exceeded patrol range from spawn point (accounting for collider size)
        float distFromSpawn = transform.position.x - _spawnPos.x;
        float halfWidth = patrolWidth / 2f;
        float colliderOffset = (_collider != null) ? _collider.bounds.extents.x : 0f;
        float patrolLimit = Mathf.Max(0f, halfWidth - colliderOffset);

        if (Mathf.Abs(distFromSpawn) >= patrolLimit)
        {
            if (Mathf.Sign(distFromSpawn) == Mathf.Sign(_facingDir))
            {
                Flip();
            }
        }

        DetectPlayer();
    }

    private bool IsPlayerInDetectionRange()
    {
        if (_playerTransform == null) return false;

        // Target the player's chest (approx 1.2 units above pivot/feet)
        Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);
        Vector2 diff = targetPos - (Vector2)transform.position;
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

        Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);

        // If either the BEE itself or the player exits the leash zone, return to Patrol
        if (!IsInLeashZone(transform.position) || !IsInLeashZone(targetPos))
        {
            _currentState = State.Patrol;
            return;
        }

        // Target the player's chest (approx 1.2 units above pivot/feet)
        Vector2 diffToBee = targetPos - (Vector2)transform.position;
        float xDist = Mathf.Abs(diffToBee.x);

        // Face player horizontally
        float dirToPlayer = Mathf.Sign(diffToBee.x);
        if (dirToPlayer != _facingDir)
        {
            Flip();
        }

        // Fly towards player horizontally and match vertical level smoothly with avoidance
        float targetVx = _facingDir * chargeSpeed;
        float targetVy = (targetPos.y + _obstacleYOffset - transform.position.y) * 4f;
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
        // Float on Y axis while standing still horizontally with avoidance (offset built-in)
        float targetVy = (targetY - transform.position.y) * 4f;
        _rb.linearVelocity = new Vector2(0f, targetVy);

        _stateTimer -= Time.fixedDeltaTime;
        if (_stateTimer <= 0f)
        {
            // After cooldown, re-aggro if both the bee and the player are inside the leash zone
            if (_playerTransform != null)
            {
                Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);
                if (IsInLeashZone(transform.position) && IsInLeashZone(targetPos))
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
            else if (_currentState == State.Patrol)
            {
                // Only flip if we are actually facing towards the player to prevent jittering when overlapping
                float dirToPlayer = Mathf.Sign(collision.transform.position.x - transform.position.x);
                if (dirToPlayer == Mathf.Sign(_facingDir))
                {
                    Flip();
                }
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
                // Only flip if we hit a wall in front of us
                bool hitWallInFront = false;
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    if (Mathf.Abs(contact.normal.x) > 0.7f)
                    {
                        // Normal points away from the wall surface.
                        // If we are moving towards the wall, normal.x and _facingDir must have opposite signs.
                        if (Mathf.Sign(contact.normal.x) != Mathf.Sign(_facingDir))
                        {
                            hitWallInFront = true;
                            break;
                        }
                    }
                }
                if (hitWallInFront)
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
            // Re-aggro if both the bee and the player are inside the leash zone
            if (_playerTransform != null)
            {
                Vector2 targetPos = new Vector2(_playerTransform.position.x, _playerTransform.position.y + 1.2f);
                if (IsInLeashZone(transform.position) && IsInLeashZone(targetPos))
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
        // Keep the editor viewport clean when the bee is not selected.
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)_spawnPos : transform.position;

        // Bright solid cyan for selected patrol zone
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(patrolWidth, patrolHeight, 0f));

        // Bright yellow for selected leash boundary (centered at spawn, based on detection range)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
        Gizmos.DrawWireCube(center, new Vector3(detectionWidth * leashMultiplier, detectionHeight * leashMultiplier, 0f));

        // Solid orange for attack range trigger (moves with the bee)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos - new Vector3(attackRange, 0f, 0f), pos + new Vector3(attackRange, 0f, 0f));
        Gizmos.DrawLine(pos - new Vector3(attackRange, 0f, 0f) + Vector3.up * 0.15f, pos - new Vector3(attackRange, 0f, 0f) + Vector3.down * 0.15f);
        Gizmos.DrawLine(pos + new Vector3(attackRange, 0f, 0f) + Vector3.up * 0.15f, pos + new Vector3(attackRange, 0f, 0f) + Vector3.down * 0.15f);

        // Real-time player detection range centered on the bee (soft purple-blue)
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.4f);
        Gizmos.DrawWireCube(pos, new Vector3(detectionWidth, detectionHeight, 0f));

        // Draw obstacle avoidance probes
        float bodyRadius = 0.4f;
        if (_collider is CircleCollider2D circle)
        {
            bodyRadius = circle.radius * Mathf.Abs(transform.localScale.x);
        }
        int dir = transform.localScale.x < 0 ? 1 : -1;
        if (Application.isPlaying) dir = _facingDir;

        Vector2 origin = (Vector2)transform.position + new Vector2(dir * bodyRadius, 0f);
        Vector2 direction = new Vector2(dir, 0f);

        // Wall check rays
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, direction * obstacleCheckDistance);
        Gizmos.DrawRay(origin + Vector2.up * upperRayOffset, direction * obstacleCheckDistance);

        // Downward height probes
        Gizmos.color = Color.green;
        Vector2 probeOriginBelow = (Vector2)transform.position + Vector2.up * probeHeight;
        Vector2 probeOriginAhead = probeOriginBelow + new Vector2(dir * lookAheadDistance, 0f);
        float rayLength = probeHeight + probeDepth;
        Gizmos.DrawRay(probeOriginBelow, Vector2.down * rayLength);
        Gizmos.DrawRay(probeOriginAhead, Vector2.down * rayLength);
    }

    #if UNITY_EDITOR
    public void Test_SetState(string stateName)
    {
        _currentState = (State)System.Enum.Parse(typeof(State), stateName);
    }

    public void Test_SetSpawnPos(Vector2 pos)
    {
        _spawnPos = pos;
    }
    #endif
}
