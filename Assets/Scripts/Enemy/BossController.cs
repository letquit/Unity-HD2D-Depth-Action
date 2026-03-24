using UnityEngine;
using UnityEngine.InputSystem;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SphereCollider detectionZone;
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bossSprite;

    [Header("Settings")]
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement; 

    private bool isPlayerInRange = false;
    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    
    [Header("Debug")]
    private EnemyHealth enemyHealth;
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    private void Start()
    {
        isDead = false;
        isAttacking = false;
        isPlayerInRange = false;
        
        enemyHealth = GetComponent<EnemyHealth>();
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (detectionZone == null) detectionZone = GetComponent<SphereCollider>();
        if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<MeleeHitbox>();
        if (bossSprite == null && animator != null) bossSprite = animator.transform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        if (animator != null)
        {
            animator.ResetTrigger("BossAttack");
            animator.ResetTrigger("BossStartDie");
            animator.Play("Boss1_Idle", 0, 0f);
        }
    }

    private void Update()
    {
        if (isDead && Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ReviveBossDebug();
            return;
        }
        if (isDead) return;

        UpdatePlayerInRange();

        if (isPlayerInRange)
            FaceToPlayer();

        if (isPlayerInRange && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
            StartAttack();
    }
    
    private void ReviveBossDebug()
    {
        isDead = false;
        isAttacking = false;
        isPlayerInRange = false;
        lastAttackTime = Time.time;

        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (detectionZone != null) detectionZone.enabled = true;
        if (meleeHitbox != null) meleeHitbox.ForceReset();

        if (enemyHealth != null) enemyHealth.ResetToFull();

        if (animator != null)
        {
            animator.ResetTrigger("BossStartDie");
            animator.ResetTrigger("BossAttack");
            animator.Play("Boss1_Idle", 0, 0f);
        }

        gameObject.SetActive(true);
    }
    
    public bool IsPlayerDead()
    {
        return playerMovement != null && playerMovement.IsDead();
    }

    private void UpdatePlayerInRange()
    {
        if (player == null || detectionZone == null || IsPlayerDead())
        {
            isPlayerInRange = false;
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(detectionZone.center);
        float worldRadius = detectionZone.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        isPlayerInRange = Vector3.Distance(player.position, worldCenter) <= worldRadius;
    }
    
    private void FaceToPlayer()
    {
        if (player == null || bossSprite == null) return;

        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) < 0.01f) return;

        bossSprite.rotation = Quaternion.Euler(0f, deltaX > 0 ? 0f : 180f, 0f);
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
    
    public void OnAttackHitboxEnter()
    {
        if (playerMovement != null)
        {
            playerMovement.OnBossAttackHitboxEnter();
        }
    }
    
    public void OnAttackHitboxExit()
    {
        if (playerMovement != null)
        {
            playerMovement.OnBossAttackHitboxExit();
        }
        
        if (meleeHitbox != null) meleeHitbox.ForceReset();
    }

    private void OnAttackHitboxEnter(Collider other)
    {
        if (!isAttacking)
        {
            return;
        }
    
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (playerMovement != null)
            {
                playerMovement.OnBossAttackHit();
            }
        }
    }
    
    private void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator?.SetTrigger("BossAttack");
    }

    private void StopAttack()
    {
        isAttacking = false;
        if (meleeHitbox != null) meleeHitbox.ForceReset();
    }

    public void OnAttackStart()
    {
        if (meleeHitbox != null) meleeHitbox.OnAttackStart();
    }

    public void OnAttackEnd()
    {
        if (meleeHitbox != null) meleeHitbox.ForceReset();
        isAttacking = false;
    }

    public void OnBossDie()
    {
        if (isDead) return;
        isDead = true;
        isPlayerInRange = false;
        isAttacking = false;

        if (detectionZone != null) detectionZone.enabled = false;
        if (meleeHitbox != null) meleeHitbox.ForceReset();

        animator?.SetTrigger("BossStartDie");
    }

    public void BossIsDead()
    {
        // var healthBar = GetComponent<EnemyHealth>()?.GetComponentInChildren<HealthBar>();
        // if (healthBar != null) Destroy(healthBar.gameObject);
        // Destroy(gameObject);
    }
}