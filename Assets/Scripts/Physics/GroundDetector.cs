using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GroundDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkDistance = 0.05f;
    [SerializeField, Range(0.1f, 1f)] private float checkWidthPercent = 0.9f;

    private Collider2D _collider;

    public bool IsGrounded { get; private set; }

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        IsGrounded = CheckGrounded();
    }

    public bool IsSafelyInland()
    {
        if (_collider == null) return false;
        Bounds bounds = _collider.bounds;
        
        // Используем ширину проверки, чтобы не учитывать закругленные края коллайдера
        float width = bounds.size.x * checkWidthPercent;
        Vector2 leftOrigin = new Vector2(bounds.center.x - width / 2f, bounds.min.y + 0.1f);
        Vector2 rightOrigin = new Vector2(bounds.center.x + width / 2f, bounds.min.y + 0.1f);
        
        float dist = checkDistance + 0.2f; // Чуть длиннее, чтобы точно задеть землю
        
        bool hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, dist, groundLayer);
        bool hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, dist, groundLayer);
        
        return hitLeft && hitRight;
    }

    private bool CheckGrounded()
    {
        if (_collider == null) return false;

        Bounds bounds = _collider.bounds;
        return Physics2D.BoxCast(
            new Vector2(bounds.center.x, bounds.min.y),
            new Vector2(bounds.size.x * checkWidthPercent, 0.02f),
            0f,
            Vector2.down,
            checkDistance,
            groundLayer
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider == null) return;

        Bounds bounds = _collider.bounds;
        Vector2 raycastOrigin = new Vector2(bounds.center.x, bounds.min.y - (checkDistance / 2f));
        Vector2 boxSize = new Vector2(bounds.size.x * checkWidthPercent, checkDistance);

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(raycastOrigin, boxSize);
    }
}
