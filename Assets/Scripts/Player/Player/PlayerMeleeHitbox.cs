using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeHitbox : MonoBehaviour
{
    public LayerMask targetLayer;
    public int damage = 10;
    
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private BoxCollider weaponCollider;

    private void Awake()
    {
        weaponCollider = GetComponent<BoxCollider>();
        if (weaponCollider == null)
        {
            Debug.LogError("[PlayerMeleeHitbox] BoxCollider not found!");
            return;
        }
        
        weaponCollider.isTrigger = true;
        weaponCollider.enabled = false;
    }

    public void OnAttackStart()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }
    
        if (((1 << other.gameObject.layer) & targetLayer) == 0)
        {
            return;
        }
    
        if (hitTargets.Contains(other.gameObject))
            return;
    
        hitTargets.Add(other.gameObject);
    
        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage, gameObject);
        }
    }
    
    public void ForceReset()
    {
        hitTargets.Clear();
        weaponCollider.enabled = false;
    }
}