using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private BossController bossController;

    [SerializeField] private GameObject VFXObject;

    private void Start()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
        bossController = GetComponentInParent<BossController>();
    }

    public void OnAttackBegin()
    {
        VFXObject.SetActive(true);
    }

    public void OnAttackReady()
    {
        VFXObject.SetActive(false);
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
    
    public void OnAttackHitboxEnter()
    {
        if (bossController != null)
        {
            bossController.OnAttackHitboxEnter();
        }
    }
    
    public void OnAttackHitboxExit()
    {
        if (bossController != null)
        {
            bossController.OnAttackHitboxExit();
        }
    }
}