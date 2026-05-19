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
        private Vector3 _lastCameraPosition;
        private float _textureUnitSizeX;
        private float _textureUnitSizeY;

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
                _lastCameraPosition = _cameraTransform.position;
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
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            Vector3 deltaMovement = _cameraTransform.position - _lastCameraPosition;
            
            // Двигаем фон в зависимости от перемещения камеры и множителя
            // parallaxMultiplier определяет, насколько фон СЛЕДУЕТ за камерой.
            // Если multiplier = 1, фон полностью привязан к камере (не отстает, нет параллакса).
            // Если multiplier = 0, фон стоит на месте.
            transform.position += new Vector3(deltaMovement.x * parallaxMultiplier.x, deltaMovement.y * parallaxMultiplier.y, 0);
            
            _lastCameraPosition = _cameraTransform.position;

            // Логика бесконечной прокрутки (тайлинг)
            if (infiniteHorizontal && _textureUnitSizeX > 0)
            {
                if (Mathf.Abs(_cameraTransform.position.x - transform.position.x) >= _textureUnitSizeX)
                {
                    float offsetPositionX = (_cameraTransform.position.x - transform.position.x) % _textureUnitSizeX;
                    transform.position = new Vector3(_cameraTransform.position.x - offsetPositionX, transform.position.y, transform.position.z);
                }
            }

            if (infiniteVertical && _textureUnitSizeY > 0)
            {
                if (Mathf.Abs(_cameraTransform.position.y - transform.position.y) >= _textureUnitSizeY)
                {
                    float offsetPositionY = (_cameraTransform.position.y - transform.position.y) % _textureUnitSizeY;
                    transform.position = new Vector3(transform.position.x, _cameraTransform.position.y - offsetPositionY, transform.position.z);
                }
            }
        }
    }
}
