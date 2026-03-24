using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float runSpeedMultiplier = 2f;
    public float dashPressThreshold = 0.2f;
    public bool canTurnWhileRunning = true;

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
    public TextMeshProUGUI blockResultText;
    public float perfectBlockWindow = 0.2f;
    public float blockFailureKnockback = 2f;
    public float hitKnockback = 4f;
    public float knockbackForce = 10f;
    public PlayerData playerData;
    
    [Header("UI")]
    [SerializeField] private Image playerHealthBar;
    [SerializeField] private bool smoothHealthBar = false;
    [SerializeField] private float healthBarSmoothSpeed = 5f;

    private float currentHealthBarFill = 1f;
    
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 1f;
    [Tooltip("耐力回复速率")]
    [SerializeField] private float staminaRegenRate = 0.1f;
    [SerializeField] private float staminaCostRunning = 0.2f;
    [SerializeField] private float staminaCostJump = 0.12f;
    [SerializeField] private float staminaCostBlocking = 0.15f;
    [SerializeField] private float staminaCostDash = 0.25f;
    [SerializeField] private float staminaCostAttack = 0.15f;
    [SerializeField] private Image playerStaminaBar;
    [Tooltip("单点弹反耐力")]
    [SerializeField] private float staminaCostBlockTap = 0.1f;
    [Tooltip("完美格挡耐力")]
    [SerializeField] private float staminaCostPerfectBlock = 0.15f;
    [Tooltip("格挡失败耐力百分比")]
    [SerializeField] private float staminaLossBlockFailPercent = 0.3f;
    [Tooltip("未格挡回复耐力百分比")]
    [SerializeField] private float staminaAddNoBlockPercent = 0.3f;
    
    [Header("blockMsgShowTime")]
    [SerializeField] private float blockMsgDuration = 0.8f;
    private float blockMsgUntil = 0f;
    
    [Header("Combat Assist")]
    public bool enableAutoAimAttackToBoss = false;
    
    private bool blockPressDenied = false;
    private float blockPressStartTime = 0f;
    
    private float currentStamina;
    private bool runExhaustedLock = false;
    private bool blockExhaustedLock = false;

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
    private bool isRunning = false;
    private float shiftPressStartTime = 0f;
    private InputAction sprintAction;
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
    
    private float bossAttackHitboxEnterTime = -100f;

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
        
        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = 1f;
            currentHealthBarFill = 1f;
            playerHealthBar.type = Image.Type.Filled;
            playerHealthBar.fillMethod = Image.FillMethod.Horizontal;
            playerHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        
        currentStamina = maxStamina;
    
        if (playerStaminaBar != null)
        {
            playerStaminaBar.fillAmount = 1f;
            playerStaminaBar.type = Image.Type.Filled;
            playerStaminaBar.fillMethod = Image.FillMethod.Horizontal;
            playerStaminaBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        
        attackAction = playerInput.actions["Attack"];
        blockAction = playerInput.actions["Block"]; 
        sprintAction = playerInput.actions["Dash"];
        
        attackAction.started += OnAttackStarted;
        attackAction.canceled += OnAttackCanceled;
        
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
        
        if (!isRunning && !isBlocking && !isDashing && !isAttacking && !isCharging && !isBeingHit)
        {
            RegenStamina(staminaRegenRate * Time.deltaTime);
        }
    
        if (isRunning)
        {
            if (!ConsumeStamina(staminaCostRunning * Time.deltaTime))
            {
                StopRunning();
                runExhaustedLock = true;
            }
        }
    
        if (isBlocking)
        {
            if (!ConsumeStamina(staminaCostBlocking * Time.deltaTime))
            {
                StopBlocking();
                blockExhaustedLock = true;
            }
        }
        
        if (isCharging)
        {
            if (chargeTimer >= 0.1f)
            {
                if (!ConsumeStamina(staminaCostAttack * Time.deltaTime))
                {
                    isCharging = false;
                    isAttacking = true;
                    
                    animator.SetTrigger(CharacterAnimations.ChargeAttack);
                    ApplyChargeEffect(GetChargeLevel(chargeTimer));
                    
                    HideChargeUI();
                }
            }
        }
        
        if (smoothHealthBar && playerHealthBar != null && 
            Mathf.Abs(currentHealthBarFill - playerData.HP / playerData.maxHP) > 0.001f)
        {
            UpdateHealthBar();
        }
        
        HandleSprintInput();
        
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
        
        if (isBlocking)
        {
            blockResultText.text = "正在格挡";
        }
        else if (blockResultText != null && !string.IsNullOrEmpty(blockResultText.text) && Time.time >= blockMsgUntil)
        {
            blockResultText.text = "";
        }
    }
    
    private void ShowBlockMsg(string msg)
    {
        if (blockResultText == null) return;
        blockResultText.text = msg;
        blockMsgUntil = Time.time + blockMsgDuration;
    }
    
    private void HandleSprintInput()
    {
        if (isDead || isAttacking || isBlocking || isCharging || isBeingHit) return;
        if (sprintAction == null) return;

        if (sprintAction.WasPressedThisFrame())
        {
            shiftPressStartTime = Time.time;
        }
        
        if (sprintAction.WasReleasedThisFrame())
        {
            runExhaustedLock = false;

            float held = Time.time - shiftPressStartTime;
            if (isRunning)
            {
                StopRunning();
            }
            else
            {
                if (held < dashPressThreshold || moveInput.magnitude <= 0.01f)
                {
                    TryStartDash();
                }
            }
            shiftPressStartTime = 0f;
            return;
        }

        if (runExhaustedLock) return;
        
        if (sprintAction.IsPressed() && !isRunning && !isDashing && moveInput.magnitude > 0.01f)
        {
            float held = Time.time - shiftPressStartTime;
            if (held >= dashPressThreshold)
            {
                StartRunning();
            }
        }

        if (isRunning && (!sprintAction.IsPressed() || moveInput.magnitude < 0.01f))
        {
            StopRunning();
        }
    }
    
    private void StartRunning()
    {
        if (isRunning || isDashing) return;
    
        isRunning = true;
    }

    private void StopRunning()
    {
        if (!isRunning) return;
    
        isRunning = false;
    }

    private void TryStartDash()
    {
        if (isDead || isAttacking || isBlocking || isCharging || isBeingHit) return;
        if (isRunning) StopRunning();
        if (dashCooldownTimer > 0f) return;
        
        if (!ConsumeStaminaInstant(staminaCostDash))
        {
            return;
        }
    
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
    
    private void HandleBlockInput()
    {
        if (isDead || isAttacking || isCharging || isDashing || isBeingHit) return;
        if (blockAction == null) return;

        if (blockAction.WasPressedThisFrame())
        {
            blockPressStartTime = Time.time;
            blockPressDenied = false;

            float minNeeded = 0.01f + staminaCostBlockTap;
            if (currentStamina < minNeeded)
            {
                blockPressDenied = true;
                ShowBlockMsg("耐力不足");
                return;
            }
        }
        
        if (!blockAction.IsPressed())
        {
            blockExhaustedLock = false;

            if (isBlocking)
            {
                // float blockDuration = Time.time - blockPressStartTime;
                // if (blockDuration < blockTapThreshold && blockDuration > 0.05f)
                // {
                //     if (ConsumeStaminaInstant(staminaCostBlockTap))
                //         Debug.Log($"[Block] Tap block SUCCESS! Cost: {staminaCostBlockTap}");
                //     else
                //         Debug.Log("[Block] Tap block FAILED: Not enough stamina!");
                // }
                StopBlocking();
            }

            blockPressDenied = false;
            return;
        }

        if (blockPressDenied) return;
        if (blockExhaustedLock) return; 

        if (blockAction.IsPressed() && !isBlocking)
        {
            StartBlocking();
        }
    }

    private void StartBlocking()
    {
        if (currentStamina <= 0.01f)
        {
            return;
        }
        
        isBlocking = true;
        blockStartTime = Time.time;
        
        animator.SetBool(CharacterAnimations.IsBlocking, true);
        
        animator.SetTrigger(CharacterAnimations.StartBlock);
    }

    private void StopBlocking()
    {
        isBlocking = false;
    
        animator.SetBool(CharacterAnimations.IsBlocking, false);
    
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
    
    public void OnBossAttackHitboxEnter()
    {
        bossAttackHitboxEnterTime = Time.time;
    }

    public void OnBossAttackHitboxExit()
    {
        if (isBlocking && blockResultText.text == "正在格挡")
        {
            ShowBlockMsg("格挡成功");
        }
    }
    
    public void OnBossAttackHit()
    {
        if (isDashing)
        {
            Debug.Log("Dash invincibility - Attack missed!");
            return;
        }
    
        Vector3 bossPosition = FindBossPosition();
        bool isFacingBoss = IsPlayerFacingBoss(bossPosition);
    
        if (isBlocking && isFacingBoss)
        {
            float timeUntilHitboxEnter = bossAttackHitboxEnterTime - blockStartTime;
        
            if (timeUntilHitboxEnter >= 0f && timeUntilHitboxEnter <= perfectBlockWindow)
            {
                Debug.Log("PERFECT BLOCK!");
            
                if (ConsumeStaminaInstant(staminaCostPerfectBlock))
                {
                    animator.SetTrigger(CharacterAnimations.BlockSuccess);
                    ShowBlockMsg("完美格挡");
                    Debug.Log($"[Perfect Block] Stamina: {currentStamina:F2}");
                }
                else
                {
                    Debug.Log("[Perfect Block] FAILED: Not enough stamina!");
                    ShowBlockMsg("耐力不足\n降为普通格挡");
                
                    float staminaNotEnoughLoss = maxStamina * staminaLossBlockFailPercent;
                    currentStamina = Mathf.Max(0f, currentStamina - staminaNotEnoughLoss);
                    UpdateStaminaBar();
                
                    animator.SetTrigger(CharacterAnimations.BlockFailure);
                    TakeDamage(10, blockFailureKnockback, bossPosition);
                }
                return;
            }

        
            Debug.Log("Block Success");
            ShowBlockMsg("普通格挡");
            
            float staminaLoss = maxStamina * staminaLossBlockFailPercent;
            currentStamina = Mathf.Max(0f, currentStamina - staminaLoss);
            UpdateStaminaBar();
        
            TakeDamage(10, blockFailureKnockback, bossPosition);
            return;
        }
    
        bool isBackToBoss = !isFacingBoss;
        Debug.Log(isBackToBoss ? "HIT! (Back to boss)" : "HIT! (No block)");
    
        if (isCharging)
        {
            isCharging = false;
            hasEnteredChargeAnim = false;
            HideChargeUI();
        }
        
        float staminaGain = maxStamina * staminaAddNoBlockPercent;
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaGain);
        UpdateStaminaBar();
    
        TakeDamage(10, hitKnockback, bossPosition);
    }

    private Vector3 FindBossPosition()
    {
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss != null)
        {
            return boss.transform.position;
        }
        
        return transform.position;
    }

    private bool IsPlayerFacingBoss(Vector3 bossPosition)
    {
        Vector3 toBoss = (bossPosition - transform.position).normalized;
    
        float rotationY = spriteTransform.rotation.eulerAngles.y;
        bool isFacingLeft = rotationY > 90f && rotationY < 270f;
    
        Vector3 playerForward = isFacingLeft ? Vector3.left : Vector3.right;
    
        float dotProduct = Vector3.Dot(playerForward, toBoss);
    
        return dotProduct > 0.5f;
    }
    
    private void UpdateStaminaBar()
    {
        if (playerStaminaBar == null) return;

        float targetFill = Mathf.Clamp01(currentStamina / maxStamina);
        playerStaminaBar.fillAmount = targetFill;
    }

    private bool ConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateStaminaBar();
            return true;
        }
        return false;
    }

    private void RegenStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        UpdateStaminaBar();
    }
    
    private bool ConsumeStaminaInstant(float amount)
    {
        if (currentStamina < amount) return false;
    
        currentStamina -= amount;
        currentStamina = Mathf.Max(0f, currentStamina);
    
        UpdateStaminaBar();
    
        return true;
    }
    
    private void HideChargeUI()
    {
        if (chargeBarFrame != null) chargeBarFrame.SetActive(false);
        if (chargeBar != null)
        {
            chargeBar.gameObject.SetActive(false);
            chargeBar.fillAmount = 0f;
        }
        isChargeBarVisible = false;
        hasEnteredChargeAnim = false;
    }
    
    private void UpdateHealthBar()
    {
        if (playerHealthBar == null) return;
    
        float targetFill = Mathf.Clamp01(playerData.HP / playerData.maxHP);
    
        if (smoothHealthBar)
        {
            currentHealthBarFill = Mathf.MoveTowards(currentHealthBarFill, targetFill, 
                healthBarSmoothSpeed * Time.deltaTime);
            playerHealthBar.fillAmount = currentHealthBarFill;
        
            if (Mathf.Abs(currentHealthBarFill - targetFill) < 0.001f)
            {
                currentHealthBarFill = targetFill;
                playerHealthBar.fillAmount = targetFill;
            }
        }
        else
        {
            playerHealthBar.fillAmount = targetFill;
            currentHealthBarFill = targetFill;
        }
    }

    private void TakeDamage(int damage, float knockbackDistance, Vector3 bossPosition)
    {
        if (isDashing) return;
    
        playerData.HP -= damage;
        Debug.Log($"HP: {playerData.HP}/{playerData.maxHP}");
    
        UpdateHealthBar();
    
        if (playerData.HP <= 0)
        {
            playerData.HP = 0;
        
            if (playerHealthBar != null)
            {
                playerHealthBar.fillAmount = 0f;
                currentHealthBarFill = 0f;
            }
        
            Die();
            return;
        }
    
        EnterHitState(knockbackDistance, bossPosition);
    }

    private void TakeDamage(int damage, float knockbackDistance)
    {
        TakeDamage(damage, knockbackDistance, FindBossPosition());
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
    
        HideChargeUI();
    }
    
    private void ForceReleaseChargeAttack()
    {
        isCharging = false;
    
        ChargeLevel finalLevel = ChargeLevel.L6;
    
        animator.SetTrigger(CharacterAnimations.ChargeAttack);
        isAttacking = true;
    
        ApplyChargeEffect(finalLevel);
    
        HideChargeUI();
    }
    
    private void AutoAimToBossOnAttack()
    {
        if (!enableAutoAimAttackToBoss) return;
        if (spriteTransform == null) return;

        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss == null) return;

        float dx = boss.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        spriteTransform.rotation = Quaternion.Euler(0f, dx > 0f ? 0f : 180f, 0f);
    }

    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (isDead || isAttacking || isDashing || isBeingHit) return;
        
        AutoAimToBossOnAttack();

        if (currentStamina < staminaCostAttack) return;

        isCharging = true;
        chargeTimer = 0f;
        hasEnteredChargeAnim = false;
        lastChargeLevel = -1;
        isAutoReleased = false;
        
        var playerHitbox = GetComponentInChildren<PlayerMeleeHitbox>();
        if (playerHitbox != null)
        {
            playerHitbox.OnAttackStart();
        }
        
        if (chargeBar != null)
        {
            chargeBar.fillAmount = 0f;
        }
        isChargeBarVisible = false;
    }

    private void OnAttackCanceled(InputAction.CallbackContext ctx)
    {
        if (isDead || isAttacking) return;
        if (isBeingHit) return;
        if (!isCharging) return;
        
        if (isAutoReleased) return;

        isCharging = false;

        ChargeLevel finalLevel = GetChargeLevel(chargeTimer);
    
        if (chargeTimer < 0.5f)
        {
            if (!ConsumeStaminaInstant(staminaCostAttack))
            {
                return; 
            }
            animator.SetTrigger(CharacterAnimations.Attack);
        }
        else
        {
            if (currentStamina < 0f) currentStamina = 0f;
            animator.SetTrigger(CharacterAnimations.ChargeAttack);
        }
    
        isAttacking = true;
    
        ApplyChargeEffect(finalLevel);
        
        HideChargeUI();
    }

    private void OnMove(InputValue value)
    {
        if (isDead) return;

        moveInput = value.Get<Vector2>();

        if (!isBeingHit)
        {
            animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
        
            if (moveInput.magnitude > 0.01f)
            {
                lastMoveDirection = moveInput;
            }
        
            if (isRunning && canTurnWhileRunning && spriteTransform != null && moveInput.x != 0)
            {
                spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
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
        else
        {
            if (moveInput.magnitude > 0.01f)
            {
                lastMoveDirection = moveInput;
            }
        }
    }

    private void OnJump(InputValue value)
    {
        if (isDead || isAttacking || isBlocking || isCharging) return;

        if (value.isPressed && isGrounded && !isDashing)
        {
            if (!ConsumeStaminaInstant(staminaCostJump))
            {
                return;
            }
            
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
        
        var playerHitbox = GetComponentInChildren<PlayerMeleeHitbox>();
        if (playerHitbox != null)
        {
            playerHitbox.ForceReset();
        }
        
        if (spriteTransform != null && moveInput.x != 0)
        {
            spriteTransform.rotation = Quaternion.Euler(0, moveInput.x > 0 ? 0f : 180f, 0);
        }
        
        HideChargeUI();
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    private void EnterHitState(float knockbackDistance, Vector3 bossPosition)
    {
        isCharging = false;
        isAttacking = false;
        isBlocking = false;
        hasEnteredChargeAnim = false;
    
        isRunning = false;
        shiftPressStartTime = 0f;
    
        animator.SetBool(CharacterAnimations.IsBlocking, false);
    
        animator.ResetTrigger(CharacterAnimations.Attack);
        animator.ResetTrigger(CharacterAnimations.ChargeAttack);
        animator.ResetTrigger(CharacterAnimations.StartCharge);
        animator.ResetTrigger(CharacterAnimations.BlockSuccess);
        animator.ResetTrigger(CharacterAnimations.BlockFailure);
    
        HideChargeUI();
    
        if (chargeBar != null)
        {
            chargeBarFrame.SetActive(false);
            chargeBar.gameObject.SetActive(false);
            isChargeBarVisible = false;
        }
    
        blockResultText.text = "";
    
        isBeingHit = true;
        knockbackTimer = knockbackDuration;
        
        animator.SetFloat(CharacterAnimations.Speed, 0f);
    
        if (isRunning)
        {
            StopRunning();
        }
    
        Vector3 toPlayer = (transform.position - bossPosition).normalized;
    
        if (Mathf.Abs(toPlayer.x) < 0.1f)
        {
            float rotationY = spriteTransform.rotation.eulerAngles.y;
            bool isFacingLeft = rotationY > 90f && rotationY < 270f;
            knockbackDirection = (isFacingLeft ? Vector3.right : Vector3.left) * knockbackDistance;
        }
        else
        {
            knockbackDirection = new Vector3(
                Mathf.Sign(toPlayer.x) * knockbackDistance,
                0,
                0
            );
        }
    
        animator.SetTrigger(CharacterAnimations.Hit);
    }

    private void EnterHitState(float knockbackDistance)
    {
        EnterHitState(knockbackDistance, FindBossPosition());
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
        
        animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
        
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
        
        currentStamina = maxStamina;
        UpdateStaminaBar();

        UpdateHealthBar(); 
        
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
        
        isRunning = false;
        shiftPressStartTime = 0f;
        
        HideChargeUI();

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
                    
                    animator.SetFloat(CharacterAnimations.Speed, moveInput.magnitude);
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
                float currentMoveSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
            
                rb.linearVelocity = new Vector3(
                    moveInput.x * currentMoveSpeed, 
                    rb.linearVelocity.y, 
                    moveInput.y * currentMoveSpeed
                );
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
        
        isRunning = false;
        shiftPressStartTime = 0f;

        HideChargeUI();
        
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