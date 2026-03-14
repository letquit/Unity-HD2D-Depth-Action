using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("Charge Attack")] 
    public GameObject chargeBarFrame;
    public TextMeshProUGUI chargeBarText;
    public Image chargeBar;
    private bool isChargeBarVisible = false;
    public float chargeThreshold = 0.5f;
    public float maxChargeTime = 5.1f;
    public bool logChargeTime = true;
    public bool logChargeLevel = true;
    public float chargeMoveSpeedMultiplier = 0.3f;

    private PlayerInput playerInput;
    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
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

    private bool isCharging = false;
    private float chargeTimer = 0f;
    private int lastChargeLevel = -1;
    private bool isAutoReleased = false; 
    
    private bool hasEnteredChargeAnim = false;

    private InputAction attackAction;
    private InputAction blockAction;

    public enum ChargeLevel
    {
        L0 = 0,  // 0.0 - 0.5s
        L1 = 1,  // 0.5 - 1.0s
        L2 = 2,  // 1.0 - 1.5s
        L3 = 3,  // 1.5 - 2.0s
        L4 = 4,  // 2.0 - 2.5s
        L5 = 5,  // 2.5 - 3.0s
        L6 = 6   // 3.0 - 5.1s
    }

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        spriteTransform = transform.GetChild(0);
        chargeBarFrame.SetActive(false);
        chargeBar.gameObject.SetActive(false);

        attackAction = playerInput.actions["Attack"];
        blockAction = playerInput.actions["Block"]; 
        
        attackAction.started += OnAttackStarted;
        attackAction.canceled += OnAttackCanceled;
    }

    private void OnDestroy()
    {
        if (attackAction != null)
        {
            attackAction.started -= OnAttackStarted;
            attackAction.canceled -= OnAttackCanceled;
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer > maxChargeTime) chargeTimer = maxChargeTime;

            if (chargeTimer >= maxChargeTime && !isAutoReleased)
            {
                isAutoReleased = true;
                ForceReleaseChargeAttack();
                return;
            }
            
            if (chargeTimer >= 3.0f && blockAction != null && blockAction.WasPerformedThisFrame())
            {
                ReleaseChargeAttackByRightClick();
                return;
            }
            
            if (chargeTimer >= 0.1f)
            {
                if (!isChargeBarVisible && chargeBar != null)
                {
                    chargeBarFrame.SetActive(true);
                    chargeBar.gameObject.SetActive(true);
                    isChargeBarVisible = true;
                }
            
                if (chargeBar != null)
                {
                    chargeBarText.text = chargeTimer.ToString("F1");
                    chargeBar.fillAmount = GetChargeFillAmount(chargeTimer);
                    if (chargeBarText != null && spriteTransform != null)
                    {
                        float rotationY = spriteTransform.rotation.eulerAngles.y;
                        bool isFacingLeft = rotationY > 90f && rotationY < 270f;
        
                        Vector3 currentLocalPos = chargeBarText.transform.localPosition;
                        float targetX = isFacingLeft ? 15.97f : 1.689f;
        
                        chargeBarText.transform.localPosition = new Vector3(targetX, currentLocalPos.y, currentLocalPos.z);
                        chargeBarText.transform.rotation = Quaternion.identity;
                    }
                }
            }
            
            if (chargeTimer >= 0.1f && !hasEnteredChargeAnim)
            {
                hasEnteredChargeAnim = true;
                animator.SetTrigger(CharacterAnimations.StartCharge);
            }

            if (logChargeLevel)
            {
                int currentLevel = (int)GetChargeLevel(chargeTimer);
                if (currentLevel != lastChargeLevel)
                {
                    lastChargeLevel = currentLevel;
                }
            }

            if (logChargeTime)
                Debug.Log($"Charge Time: {chargeTimer:0.000}s");
        }
    }

    public ChargeLevel GetChargeLevel(float time)
    {
        if (time < 0.5f) return ChargeLevel.L0;
        if (time < 1.0f) return ChargeLevel.L1;
        if (time < 1.5f) return ChargeLevel.L2;
        if (time < 2.0f) return ChargeLevel.L3;
        if (time < 2.5f) return ChargeLevel.L4;
        if (time < 3.0f) return ChargeLevel.L5;
        return ChargeLevel.L6;
    }

    private void ApplyChargeEffect(ChargeLevel level)
    {
        switch (level)
        {
            case ChargeLevel.L0:
                Debug.Log("Normal Attack");
                break;
            case ChargeLevel.L1:
                Debug.Log("0.5~1.0 Attack");
                break;
            case ChargeLevel.L2:
                Debug.Log("1.0~1.5 Attack");
                break;
            case ChargeLevel.L3:
                Debug.Log("1.5~2.0 Attack");
                break;
            case ChargeLevel.L4:
                Debug.Log("2.0~2.5 Attack");
                break;
            case ChargeLevel.L5:
                Debug.Log("2.5~3.0 Attack");
                break;
            case ChargeLevel.L6:
                Debug.Log("3.0~5.1 Attack");
                break;
        }
    }
    
    private float GetChargeFillAmount(float time)
    {
        if (time <= 3.0f)
        {
            return time * 0.23f;
        }
        else
        {
            return 0.69f + (time - 3.0f) * 0.155f;
        }
    }
    
    private void ReleaseChargeAttackByRightClick()
    {
        if (chargeTimer < 3.0f || chargeTimer > maxChargeTime) return;
    
        isCharging = false;
    
        ChargeLevel finalLevel = GetChargeLevel(chargeTimer);
    
        animator.SetTrigger(CharacterAnimations.ChargeAttack);
        isAttacking = true;
    
        Debug.Log("Charge Attack Released by Right Click");
    
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            isChargeBarVisible = false;
        }
    }
    
    private void ForceReleaseChargeAttack()
    {
        isCharging = false;
    
        ChargeLevel finalLevel = ChargeLevel.L6;
    
        animator.SetTrigger(CharacterAnimations.ChargeAttack);
        isAttacking = true;
    
        ApplyChargeEffect(finalLevel);
    
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            isChargeBarVisible = false;
        }
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (isDead || isAttacking) return;
        if (isDashing) return;

        isCharging = true;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        isAutoReleased = false;
        
        if (chargeBar != null)
        {
            chargeBar.fillAmount = 0f;
        }
        isChargeBarVisible = false;
    }

    private void OnAttackCanceled(InputAction.CallbackContext ctx)
    {
        if (isDead || isAttacking) return;
        if (!isCharging) return;
        
        if (isAutoReleased) return;

        isCharging = false;

        ChargeLevel finalLevel = GetChargeLevel(chargeTimer);
    
        if (chargeTimer < 0.1f)
        {
            animator.SetTrigger(CharacterAnimations.Attack);
        }
        else if (chargeTimer < 0.5f)
        {
            animator.SetTrigger(CharacterAnimations.Attack);
        }
        else
        {
            animator.SetTrigger(CharacterAnimations.ChargeAttack);
        }
    
        isAttacking = true;
    
        ApplyChargeEffect(finalLevel);
        
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            isChargeBarVisible = false;
        }
    }

    private void OnMove(InputValue value)
    {
        if (isDead) return;

        moveInput = value.Get<Vector2>();

        animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
        
        if (moveInput.magnitude > 0.01f)
        {
            lastMoveDirection = moveInput;
        }
        
        if (isCharging)
        {
            if (chargeTimer >= 0.1f && chargeTimer < 0.5f && spriteTransform != null && moveInput.x != 0)
            {
                spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
            }
            return;
        }

        if (spriteTransform != null && moveInput.x != 0)
        {
            spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
        }
    }

    private void OnJump(InputValue value)
    {
        if (isDead || isAttacking) return;
        if (isCharging) return;

        if (value.isPressed && isGrounded && !isDashing)
        {
            isGrounded = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            animator.SetBool(CharacterAnimations.IsJumping, true);
            animator.SetTrigger(CharacterAnimations.StartJump);
        }
    }

    public void SetAttacking(bool value) => isAttacking = value;

    public void OnAttackEnd()
    { 
        isAttacking = false;
        if (isCharging) 
        {
            isCharging = false;
            hasEnteredChargeAnim = false;
        }
        
        if (spriteTransform != null && moveInput.x != 0)
        {
            spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
        }
        
        if (chargeBar != null)
        {
            chargeBar.fillAmount = 0f;
        }
    }

    private void OnDash(InputValue value)
    {
        if (isDead || isAttacking) return;
        if (isCharging) return;
        if (dashCooldownTimer > 0f) return;

        if (value.isPressed && !isDashing)
        {
            animator.SetTrigger(CharacterAnimations.Dash);

            if (moveInput.magnitude != 0)
                dashDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            else
                dashDirection = new Vector3(lastMoveDirection.x, 0, lastMoveDirection.y).normalized;

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
    }

    private void OnDie(InputValue value)
    {
        if (!value.isPressed) return;
        if (isDead) Resurrect();
        else Die();
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

        isCharging = false;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        lastMoveDirection = Vector2.right;
        isAutoReleased = false;
        
        isChargeBarVisible = false;
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            chargeBar.fillAmount = 0f;
        }

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
            if (isCharging && chargeTimer >= 0.1f && chargeTimer < 0.5f)
            {
                rb.linearVelocity = new Vector3(
                    moveInput.x * moveSpeed * chargeMoveSpeedMultiplier, 
                    rb.linearVelocity.y, 
                    moveInput.y * moveSpeed * chargeMoveSpeedMultiplier
                );
                return;
            }
            
            if (isCharging || isAttacking)
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

        isCharging = false;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        isAutoReleased = false;

        isChargeBarVisible = false;
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            chargeBar.fillAmount = 0f;
        }
        
        if (animator != null)
        {
            animator.SetBool(CharacterAnimations.Die, isDead);
            animator.SetTrigger(CharacterAnimations.StartDie);
        }
    }
}