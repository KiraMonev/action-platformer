using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    private Animator _animator;

    public bool isMoving { private get; set; }
    public bool isGrounded { private get; set; }
    public float yVelocity { private get; set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (TryGetComponent<Player>(out var player))
        {
            player.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (TryGetComponent<Player>(out var player))
        {
            player.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Die");
        }
    }

    private void FixedUpdate()
    {
        if (_animator == null) return;
        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("isGrounded", isGrounded);
        _animator.SetFloat("yVelocity", yVelocity);
    }

    public void PlayAttack()
    {
        if (_animator != null) _animator.SetTrigger("attack");
    }

    public void PlayCast()
    {
        if (_animator != null) _animator.SetTrigger("cast");
    }
}
