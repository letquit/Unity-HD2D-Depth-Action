using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private BossController bossController;

    private void Start()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
        bossController = GetComponentInParent<BossController>();
    }
    
    public void OnAttackStart()
    {
        if (bossController != null)
        {
            bossController.OnAttackStart();
        }
    }
    
    public void OnAttackEnd()
    {
        if (bossController != null)
        {
            bossController.OnAttackEnd();
        }
    }
    
    public void OnDeathAnimationEnd()
    {
        if (bossController != null)
        {
            bossController.BossIsDead();
        }
        else
        {
            Destroy(transform.parent.gameObject);
        }
    }
}