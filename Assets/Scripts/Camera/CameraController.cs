using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Отслеживание")]
    [Tooltip("Цель следования")]
    [SerializeField] private Transform target;
    
    [Tooltip("Сглаживание X / Y")]
    [SerializeField] private float smoothTimeX = 0.15f;
    [SerializeField] private float smoothTimeY = 0.25f;
    
    [Tooltip("Смещение камеры")]
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 1.5f);

    [Header("Lookahead")]
    [Tooltip("Включить опережение взгляда")]
    [SerializeField] private bool useLookahead = true;
    [Tooltip("Дистанция опережения")]
    [SerializeField] private float lookaheadDistance = 2.0f;
    [Tooltip("Скорость смещения")]
    [SerializeField] private float lookaheadSpeed = 3.0f;

    [Header("Границы")]
    [Tooltip("Использовать границы")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private Vector3 _velocity;
    private float _currentLookaheadX;
    private float _targetLookaheadX;
    
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector3 _shakeOffset;

    private void Start()
    {
        if (target == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraController: PlayerController not found in the scene.");
            }
        }
        
        if (target != null)
        {
            Vector3 targetPos = target.position + (Vector3)targetOffset;
            targetPos.z = transform.position.z;
            transform.position = targetPos;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + (Vector3)targetOffset;

        if (useLookahead)
        {
            float facingDirection = Mathf.Sign(target.localScale.x);
            _targetLookaheadX = facingDirection * lookaheadDistance;
            _currentLookaheadX = Mathf.Lerp(_currentLookaheadX, _targetLookaheadX, Time.deltaTime * lookaheadSpeed);
            targetPosition.x += _currentLookaheadX;
        }

        float newX = Mathf.SmoothDamp(transform.position.x, targetPosition.x, ref _velocity.x, smoothTimeX);
        float newY = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref _velocity.y, smoothTimeY);
        
        Vector3 nextPosition = new Vector3(newX, newY, transform.position.z);

        if (_shakeDuration > 0)
        {
            _shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
            _shakeOffset.z = 0;
            
            _shakeDuration -= Time.deltaTime;
            _shakeMagnitude = Mathf.Lerp(_shakeMagnitude, 0f, Time.deltaTime * 5f);
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }

        Vector3 finalPosition = nextPosition + _shakeOffset;

        if (useBounds)
        {
            finalPosition.x = Mathf.Clamp(finalPosition.x, minBounds.x, maxBounds.x);
            finalPosition.y = Mathf.Clamp(finalPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = finalPosition;
    }

    /// <summary>
    /// Тряска экрана
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        _shakeDuration = duration;
        _shakeMagnitude = magnitude;
    }
}