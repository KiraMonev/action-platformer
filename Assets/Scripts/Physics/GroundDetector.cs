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
