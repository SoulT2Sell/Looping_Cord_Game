using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Player player;
    private Animator animator;

    public bool isHealing;

    private void Start()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        AnimationController();
    }

    private void AnimationController()
    {
        animator.SetBool("isMoving", player.playerMovement.isMoving);

        if (isHealing)
        {
            animator.SetTrigger("healing");
            isHealing = false;
        }

        animator.SetFloat("xVelocity", player.rb.linearVelocity.x);
        animator.SetFloat("yVelocity", player.rb.linearVelocity.y);
    }
}
