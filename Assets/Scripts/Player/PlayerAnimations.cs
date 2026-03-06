using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    private Animator _animator;

    public bool isMoving { private get; set; }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        _animator.SetBool("isMoving", isMoving);
    }
}
