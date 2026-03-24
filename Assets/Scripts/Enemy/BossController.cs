using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SphereCollider detectionZone;
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bossSprite; // 新增：Boss可视模型(子物体)

    [Header("Settings")]
    [SerializeField] private float attackCooldown = 2f;

    private bool isPlayerInRange = false;
    private bool isAttacking = false;
    private bool isDead = false;
    private float lastAttackTime = 0f;

    private void Start()
    {
        isDead = false;
        isAttacking = false;
        isPlayerInRange = false;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (detectionZone == null) detectionZone = GetComponent<SphereCollider>();
        if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<MeleeHitbox>();
        if (bossSprite == null && animator != null) bossSprite = animator.transform; // 默认用动画子物体

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
        if (isDead) return;

        UpdatePlayerInRange();

        if (isPlayerInRange)
            FaceToPlayer();

        if (isPlayerInRange && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
            StartAttack();
    }

    private void UpdatePlayerInRange()
    {
        if (player == null || detectionZone == null)
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

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isPlayerInRange = false;
            if (isAttacking) StopAttack();
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
        var healthBar = GetComponent<EnemyHealth>()?.GetComponentInChildren<HealthBar>();
        if (healthBar != null) Destroy(healthBar.gameObject);
        Destroy(gameObject);
    }
}