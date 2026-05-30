using System;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [Header("Здоровье и настройка сердец")]
    [SerializeField] private float health = 3.0f;
    [SerializeField] private int numOfHearts = 3;
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart; 

    public event Action OnDeath;
    private bool _isDead;
    public bool IsDead => _isDead;

    private float _invulnTimer;
    private SpriteRenderer _sr;
    private Vector2 _lastSafePosition;
    private GroundDetector _gd;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _gd = GetComponent<GroundDetector>();
        _lastSafePosition = transform.position;
    }

    private void Update()
    {
        if (_isDead) return;

        if (_invulnTimer > 0f)
        {
            _invulnTimer -= Time.deltaTime;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? 0.3f : 0.9f;
                _sr.color = c;
            }
            if (_invulnTimer <= 0f && _sr != null)
            {
                Color c = _sr.color;
                c.a = 1f;
                _sr.color = c;
            }
        }

        if (_gd != null && _gd.IsGrounded)
        {
            if (_gd.IsSafelyInland())
            {
                _lastSafePosition = transform.position;
            }
        }
    }

    private void FixedUpdate()
    {
        if(health > numOfHearts)
        {
            health = numOfHearts;
        }
        for(int i = 0; i < hearts.Length; i++)
        {
            if (i < Mathf.RoundToInt(health))
            {
                hearts[i].sprite = fullHeart;
            }
            else
            {
                hearts[i].sprite = emptyHeart;
            }
            if (i < numOfHearts)
            {
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(int damage, bool bypassKnockback)
    {
        if (health <= 0 || _invulnTimer > 0f || _isDead) return;

        health -= damage;

        if (health <= 0 && !_isDead)
        {
            _isDead = true;
            _invulnTimer = 0f;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = 1f;
                _sr.color = c;
            }
            OnDeath?.Invoke();
            return;
        }

        _invulnTimer = 1.2f;

        if (!bypassKnockback && TryGetComponent<PlayerController>(out var controller))
        {
            float knockbackDirX = -Mathf.Sign(transform.localScale.x);
            Vector2 knockbackForce = new Vector2(knockbackDirX * 3.5f, 3.0f);
            controller.ApplyKnockback(knockbackForce, 0.2f);
        }

        CameraController cam = Camera.main?.GetComponent<CameraController>();
        if (cam != null)
        {
            cam.Shake(0.2f, 0.25f);
        }
    }

    public void RespawnAtLastSafeGround(int damage)
    {
        if (_isDead) return;

        TakeDamage(damage, true);

        if (!_isDead)
        {
            transform.position = _lastSafePosition;
            if (TryGetComponent<PlayerController>(out var controller))
            {
                controller.ResetMovementState();
            }
        }
    }

    public void OnDeathAnimationComplete()
    {
        Time.timeScale = 0f;
    }
}
