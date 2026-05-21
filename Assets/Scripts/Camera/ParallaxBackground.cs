using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("Настройки параллакса")]
        [Tooltip("Сила эффекта параллакса (0 = движется вместе с камерой, 1 = стоит на месте)")]
        [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.5f, 0.2f);
        
        [Tooltip("Должен ли фон бесконечно повторяться по горизонтали")]
        [SerializeField] private bool infiniteHorizontal = true;
        
        [Tooltip("Должен ли фон бесконечно повторяться по вертикали")]
        [SerializeField] private bool infiniteVertical = false;

        private Transform _cameraTransform;
        private Vector3 _startBackgroundPosition;
        private Vector3 _startCameraPosition;
        private bool _isInitialized = false;
        private float _textureUnitSizeX;
        private float _textureUnitSizeY;

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError("ParallaxBackground: Main Camera не найдена на сцене!");
                enabled = false;
                return;
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                _textureUnitSizeX = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
                _textureUnitSizeY = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
            }

            _startBackgroundPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            if (!_isInitialized)
            {
                _startCameraPosition = _cameraTransform.position;
                _isInitialized = true;
            }

            Vector3 travel = _cameraTransform.position - _startCameraPosition;

            float targetX = _startBackgroundPosition.x + travel.x * parallaxMultiplier.x;
            float targetY = _startBackgroundPosition.y + travel.y * parallaxMultiplier.y;

            if (infiniteHorizontal && _textureUnitSizeX > 0)
            {
                float relativeTravelX = travel.x * (1 - parallaxMultiplier.x);
                int tileOffsetX = Mathf.RoundToInt(relativeTravelX / _textureUnitSizeX);
                targetX += tileOffsetX * _textureUnitSizeX;
            }

            if (infiniteVertical && _textureUnitSizeY > 0)
            {
                float relativeTravelY = travel.y * (1 - parallaxMultiplier.y);
                int tileOffsetY = Mathf.RoundToInt(relativeTravelY / _textureUnitSizeY);
                targetY += tileOffsetY * _textureUnitSizeY;
            }

            transform.position = new Vector3(targetX, targetY, transform.position.z);
        }
    }
}
