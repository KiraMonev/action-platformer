using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    private Animator _animator;

    public bool isMoving { private get; set; }
    public bool isGrounded { private get; set; }
    public float yVelocity { private get; set; }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("isGrounded", isGrounded);
        _animator.SetFloat("yVelocity", yVelocity);
    }

    public void PlayAttack()
    {
        _animator.SetTrigger("attack");
    }
}
