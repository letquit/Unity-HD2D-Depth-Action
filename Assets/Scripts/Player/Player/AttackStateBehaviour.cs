using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var movement = animator.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.SetAttacking(true);
        
        animator.ResetTrigger(CharacterAnimations.Attack);
        
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var movement = animator.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.SetAttacking(false);
    }
}