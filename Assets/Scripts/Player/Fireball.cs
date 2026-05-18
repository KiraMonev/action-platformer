using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Fireball : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private bool _isExploding;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        
        _rb.gravityScale = 0f;
        _rb.linearVelocity = direction.normalized * speed;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
        transform.localScale = scale;

        transform.rotation = Quaternion.identity;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isExploding) return;

        if (collision.CompareTag("Player")) return;

        Explode();
    }

    private void Explode()
    {
        _isExploding = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
        }
        
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (_animator != null)
        {
            _animator.SetTrigger("explode");
        }

        Destroy(gameObject, 0.15f);
    }
}
