using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Called by the server to start attack animation on all clients
    [ClientRpc]
    public void PlayAttackClientRpc()
    {
        Debug.Log("PlayAttackClientRpc: Playing attack animation on client.");
        animator.SetBool("isAttacking", true);
    }

    public void PlayIdle()
    {
        animator.SetFloat("xVelocity", 0f);
        animator.SetBool("isAttacking", false);
    }

    public void PlayWalk()
    {
        animator.SetFloat("xVelocity", 1f);
        animator.SetBool("isAttacking", false);
    }

    // Called automatically via animation event at end of attack
    public void EndAttack()
    {
        animator.SetBool("isAttacking", false);
        Debug.Log("EndAttack: Reset isAttacking.");
    }
}
