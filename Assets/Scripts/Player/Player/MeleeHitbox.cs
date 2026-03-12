using System.Collections.Generic;
using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    public LayerMask targetLayer;
    public int damage = 10;
    
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    
    private BoxCollider weaponCollider;

    private void Awake()
    {
        weaponCollider = GetComponent<BoxCollider>();
        weaponCollider.enabled = false;
    }

    public void OnAttackStart()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) == 0) 
            return;
        
        if (hitTargets.Contains(other.gameObject))
            return;
        
        hitTargets.Add(other.gameObject);
        
        var enemyHealth = other.GetComponent<EnemyHealth>();
        
        enemyHealth?.TakeDamage(damage, gameObject);
    }
    
    public void ForceReset()
    {
        hitTargets.Clear();
        weaponCollider.enabled = false;
    }
}