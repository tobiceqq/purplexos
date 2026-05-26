using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;

    private void Reset()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (animator == null || playerController == null)
            return;

        float moveSpeed = playerController.IsClimbing ? 0 : playerController.CurrentMoveAmount;
        animator.SetFloat("Speed", moveSpeed);

        animator.SetBool("IsRunning", playerController.IsRunning);
        animator.SetFloat("Speed", playerController.CurrentMoveAmount);
        animator.SetBool("IsRolling", playerController.IsBallMode);
        animator.SetBool("IsClimbing", playerController.IsClimbing);

        animator.SetFloat("VerticalVelocity", playerController.VerticalVelocity);

        animator.SetBool("IsGrounded", playerController.IsGrounded);
    }

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
    }
    public void PlayDoubleJump()
    {
        animator.SetTrigger("DoubleJump");
    }
}