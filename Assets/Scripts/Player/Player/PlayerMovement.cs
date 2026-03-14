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

    [Header("Block & Hit")]
    public float playerMaxHp = 100f;
    public float blockMoveSpeedMultiplier = 0.3f;
    public float enemyAttackInterval = 10f; // Test
    public TextMeshProUGUI enemyAttackText; // Test
    public TextMeshProUGUI blockResultText; // Test
    public float perfectBlockWindow = 0.2f;
    public float blockFailureKnockback = 2f;
    public float hitKnockback = 4f;
    public float knockbackForce = 10f;
    public PlayerData playerData;

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
    
    private bool isBlocking = false;
    private float blockStartTime = 0f;
    private bool isBeingHit = false;
    private Vector3 knockbackDirection = Vector3.zero;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.3f;
    
    private float nextEnemyAttackTime = 0f; // Test

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

        playerData = new PlayerData();
        playerData.maxHP = playerMaxHp;
        playerData.HP = playerData.maxHP;
        
        attackAction = playerInput.actions["Attack"];
        blockAction = playerInput.actions["Block"]; 
        
        attackAction.started += OnAttackStarted;
        attackAction.canceled += OnAttackCanceled;
        
        //Testing enemy attack timer
        nextEnemyAttackTime = Time.time + enemyAttackInterval;
        UpdateEnemyAttackTimerDisplay();
        blockResultText.text = "";
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

        // Test
        UpdateEnemyAttackTimerDisplay();
        CheckEnemyAttack();
        
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
        
        HandleBlockInput();
    }
    
    private void HandleBlockInput()
    {
        if (isDead || isAttacking || isCharging || isDashing || isBeingHit) return;
        
        if (blockAction != null)
        {
            if (blockAction.IsPressed() && !isBlocking)
            {
                StartBlocking();
            }
            else if (!blockAction.IsPressed() && isBlocking)
            {
                StopBlocking();
            }
        }
    }

    private void StartBlocking()
    {
        isBlocking = true;
        blockStartTime = Time.time;
        
        animator.SetBool(CharacterAnimations.IsBlocking, true);
        
        animator.SetTrigger(CharacterAnimations.StartBlock);
        
        blockResultText.text = "开始格挡";
    }

    private void StopBlocking()
    {
        isBlocking = false;
    
        animator.SetBool(CharacterAnimations.IsBlocking, false);
    
        blockResultText.text = "";
    
        if (moveInput.magnitude > 0.01f)
        {
            animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
        
            if (spriteTransform != null && moveInput.x != 0)
            {
                spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
            }
        }
        else
        {
            animator.SetFloat(CharacterAnimations.Speed, 0f);
        }
    }

    // Test
    private void CheckEnemyAttack()
    {
        if (Time.time >= nextEnemyAttackTime && !isDead)
        {
            nextEnemyAttackTime = Time.time + enemyAttackInterval;
            
            ProcessEnemyAttack();
        }
    }
    
    private void UpdateEnemyAttackTimerDisplay()
    {
        if (enemyAttackText == null) return;
    
        float timeUntilAttack = nextEnemyAttackTime - Time.time;
        float displayTime = Mathf.Max(0f, timeUntilAttack);
        enemyAttackText.text = displayTime.ToString("F2");
    
        if (displayTime > 3f)
            enemyAttackText.color = Color.white;
        else if (displayTime > 2f)
            enemyAttackText.color = Color.green;
        else if (displayTime > perfectBlockWindow)
            enemyAttackText.color = Color.yellow;
        else
            enemyAttackText.color = Color.red;
    }
    
    private void ProcessEnemyAttack()
    {
        if (isDashing)
        {
            return;
        }
        
        if (isBlocking)
        {
            float timeSinceBlockStart = Time.time - blockStartTime;
            
            if (timeSinceBlockStart <= perfectBlockWindow)
            {
                Debug.Log("PERFECT BLOCK");
                animator.SetTrigger(CharacterAnimations.BlockSuccess);
                blockResultText.text = "完美格挡";
            }
            else
            {
                Debug.Log("Block Failed");
                animator.SetTrigger(CharacterAnimations.BlockFailure);
                TakeDamage(10, blockFailureKnockback);
            }
        }
        else
        {
            int damage = 10;
            playerData.HP -= damage;
            
            if (playerData.HP <= 0)
            {
                playerData.HP = 0;
                Die();
                return;
            }
            
            if (isCharging)
            {
                isCharging = false;
                hasEnteredChargeAnim = false;
                
                if (chargeBar != null)
                {
                    chargeBarFrame.SetActive(false);
                    chargeBar.gameObject.SetActive(false);
                    isChargeBarVisible = false;
                }
            }
        
            animator.SetTrigger(CharacterAnimations.Hit);
        
            isBeingHit = true;
            knockbackTimer = knockbackDuration;
        
            float rotationY = spriteTransform.rotation.eulerAngles.y;
            bool isFacingLeft = rotationY > 90f && rotationY < 270f;
            knockbackDirection = (isFacingLeft ? Vector3.right : Vector3.left) * hitKnockback;
        }
    }

    private void TakeDamage(int damage, float knockbackDistance)
    {
        if (isDashing)
        {
            return;
        }
        
        playerData.HP -= damage;
        Debug.Log($"HP: {playerData.HP}/{playerData.maxHP}");
        
        if (playerData.HP <= 0)
        {
            playerData.HP = 0;
            Die();
            return;
        }
        
        isBeingHit = true;
        knockbackTimer = knockbackDuration;
        
        float rotationY = spriteTransform.rotation.eulerAngles.y;
        bool isFacingLeft = rotationY > 90f && rotationY < 270f;
    
        knockbackDirection = isFacingLeft ? Vector3.right : Vector3.left;
        knockbackDirection *= knockbackDistance;
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
        if (isDead || isBeingHit) return;

        moveInput = value.Get<Vector2>();

        animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
        
        if (moveInput.magnitude > 0.01f)
        {
            lastMoveDirection = moveInput;
        }
        
        if (isBlocking)
        {
            if (spriteTransform != null && moveInput.x != 0)
            {
                spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
            }
            return;
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
        if (isDead || isAttacking || isBlocking || isCharging) return;

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
    
    public void OnBlockEnd()
    {
        if (isDead) return;
        
        if (isBlocking && blockAction != null && blockAction.IsPressed())
        {
            Debug.Log("Continue Blocking");
        }
        else
        {
            StopBlocking();
        }
    }

    public void OnBlockResultEnd()
    {
        if (isDead) return;
        
        isBeingHit = false;
        knockbackTimer = 0f;
        rb.linearVelocity = Vector3.zero;
        
        if (isBlocking)
        {
            StopBlocking();
        }
    }
    
    public void OnBlockSuccessEnd()
    {
        if (isDead) return;
        
        bool isBlockKeyPressed = blockAction != null && blockAction.IsPressed();
        
        if (isBlockKeyPressed && !isDead && !isAttacking && !isCharging && !isDashing && !isBeingHit)
        {
            animator.SetTrigger(CharacterAnimations.StartBlock);
        }
        else
        {
            if (isBlocking)
            {
                StopBlocking();
            }
        }
    }

    private void OnDash(InputValue value)
    {
        if (isDead || isAttacking || isBlocking || isCharging) return;
        if (dashCooldownTimer > 0f) return;

        if (value.isPressed && !isDashing)
        {
            animator.SetTrigger(CharacterAnimations.Dash);
            animator.SetBool(CharacterAnimations.IsDashing, true);

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
        playerData.HP = playerData.maxHP;

        animator.SetBool(CharacterAnimations.Die, false);
        animator.SetBool(CharacterAnimations.IsJumping, false);
        animator.SetFloat(CharacterAnimations.Speed, 0f);

        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundDistance, groundMask);
        isAttacking = false;
        isBlocking = false;
        isDashing = false;
        isBeingHit = false;
        dashTimer = 0f;
        moveInput = Vector2.zero;
        isCharging = false;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        isAutoReleased = false;
        knockbackTimer = 0f;
        
        isChargeBarVisible = false;
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            chargeBar.fillAmount = 0f;
        }

        rb.linearVelocity = Vector3.zero;
        
        nextEnemyAttackTime = Time.time + enemyAttackInterval;
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
            if (isBeingHit && knockbackTimer > 0)
            {
                rb.linearVelocity = new Vector3(
                    knockbackDirection.x * knockbackForce,
                    rb.linearVelocity.y,
                    knockbackDirection.z * knockbackForce
                );
                knockbackTimer -= Time.fixedDeltaTime;
                
                if (knockbackTimer <= 0)
                {
                    isBeingHit = false;
                    rb.linearVelocity = Vector3.zero;
                }
                return;
            }
            
            if (isCharging && chargeTimer >= 0.1f && chargeTimer < 0.5f)
            {
                rb.linearVelocity = new Vector3(
                    moveInput.x * moveSpeed * chargeMoveSpeedMultiplier, 
                    rb.linearVelocity.y, 
                    moveInput.y * moveSpeed * chargeMoveSpeedMultiplier
                );
                return;
            }
            
            if (isBlocking)
            {
                rb.linearVelocity = new Vector3(
                    moveInput.x * moveSpeed * blockMoveSpeedMultiplier, 
                    rb.linearVelocity.y, 
                    moveInput.y * moveSpeed * blockMoveSpeedMultiplier
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
                    animator.SetBool(CharacterAnimations.IsDashing, false);
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
        isBlocking = false;
        isBeingHit = false;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        isAutoReleased = false;
        knockbackTimer = 0f;

        isChargeBarVisible = false;
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            chargeBar.fillAmount = 0f;
        }
        
        
        if (animator != null)
        {
            animator.SetBool(CharacterAnimations.IsBlocking, false);
            animator.SetBool(CharacterAnimations.IsJumping, false);
            animator.SetFloat(CharacterAnimations.Speed, 0f);
        
            animator.SetBool(CharacterAnimations.Die, true);
            animator.SetTrigger(CharacterAnimations.StartDie);
        }
    }
}