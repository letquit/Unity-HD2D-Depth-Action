using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    private PlayerInput playerInput;
    private Rigidbody rb;
    private Vector2 moveInput;
    private Animator animator;
    private Transform spriteTransform;

    private bool isGrounded;
    private bool isDead = false;

    private bool wasGrounded = false;
    
    private bool isAttacking = false;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        spriteTransform = transform.GetChild(0);
    }

    private void OnMove(InputValue value)
    {
        if (isDead) return;

        moveInput = value.Get<Vector2>();

        if (spriteTransform != null && moveInput.x != 0)
        {
            spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
        }

        animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
    }

    private void OnJump(InputValue value)
    {
        if (isDead || isAttacking) return;

        if (value.isPressed && isGrounded && !isDashing)
        {
            isGrounded = false;
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            
            animator.SetBool(CharacterAnimations.IsJumping, true);
            animator.SetTrigger(CharacterAnimations.StartJump);
        }
    }

    public void SetAttacking(bool value)
    {
        isAttacking = value;
    }
    
    public void OnAttackEnd()
    {
        isAttacking = false;
    }
    
    private void OnAttack(InputValue value)
    {
        if (isDead || isAttacking) return;

        if (value.isPressed) 
        {
            animator.SetTrigger(CharacterAnimations.Attack);
            isAttacking = true;
        }
    }

    private void OnDash(InputValue value)
    {
        if (isDead || isAttacking) return;

        if (dashCooldownTimer > 0f) return;
        
        if (value.isPressed && !isDashing)
        {
            animator.SetTrigger(CharacterAnimations.Dash);

            if (moveInput.magnitude != 0)
            {
                dashDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            }
            else
            {
                float facing = Mathf.Sign(spriteTransform.localScale.x);
                dashDirection = new Vector3(facing, 0, 0);
            }

            isDashing = true;
            dashTimer = dashDuration;
            
            dashCooldownTimer = dashCooldown;
        }
    }

    private void OnDie(InputValue value)
    {
        if (!value.isPressed) return;
        
        if (isDead)
            Resurrect();
        else
            Die();
    }
    
    private void Resurrect()
    {
        isDead = false;
    
        animator.SetBool(CharacterAnimations.Die, false);
        animator.SetBool(CharacterAnimations.IsJumping, false);
        animator.SetFloat(CharacterAnimations.Speed, 0f);
    
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundDistance, groundMask);
        isAttacking = false;
        isDashing = false;
        dashTimer = 0f;
        moveInput = Vector2.zero;
    
        rb.linearVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        bool wasGroundedLastFrame = isGrounded;
        
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundDistance, groundMask);

        if (isGrounded && !wasGroundedLastFrame)
        {
            animator.SetBool(CharacterAnimations.IsJumping, false);
            
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
        
        if (isGrounded && animator.GetBool(CharacterAnimations.IsJumping))
        {
            animator.SetBool(CharacterAnimations.IsJumping, false);
        }

        wasGrounded = isGrounded;
        
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.fixedDeltaTime;

        if (!isDead)
        {
            if (isAttacking)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                return;
            }
            
            if (isDashing)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                    rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
                }
            }
            else
            {
                rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
            }
        }
    }
    
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(CharacterAnimations.Die, isDead);
            animator.SetTrigger(CharacterAnimations.StartDie);
        }
    }
}