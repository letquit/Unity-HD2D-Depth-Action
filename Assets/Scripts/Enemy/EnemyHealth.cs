using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private HealthBar healthBar;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (healthBar)
            healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage, GameObject instigator)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (healthBar)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} Die");
        Destroy(healthBar.gameObject);
        Destroy(gameObject);
    }
}