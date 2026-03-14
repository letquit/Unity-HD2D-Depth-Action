using UnityEngine;

public class CharacterAnimations
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int IsJumping = Animator.StringToHash("IsJumping");
    public static readonly int StartJump = Animator.StringToHash("StartJump");
    public static readonly int Die = Animator.StringToHash("Die");
    public static readonly int StartDie = Animator.StringToHash("StartDie");
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int Dash = Animator.StringToHash("Dash");
    public static readonly int IsDashing = Animator.StringToHash("IsDashing");

    public static readonly int StartCharge = Animator.StringToHash("StartCharge");
    public static readonly int ChargeAttack = Animator.StringToHash("ChargeAttack");
    
    public static readonly int StartBlock = Animator.StringToHash("StartBlock");
    public static readonly int BlockSuccess = Animator.StringToHash("BlockSuccess");
    public static readonly int BlockFailure = Animator.StringToHash("BlockFailure");
    public static readonly int Hit = Animator.StringToHash("Hit");
    public static readonly int IsBlocking = Animator.StringToHash("IsBlocking");
}