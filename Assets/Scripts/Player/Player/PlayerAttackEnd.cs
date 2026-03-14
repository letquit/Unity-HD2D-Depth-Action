using System;
using UnityEngine;

public class PlayerAttackEnd : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    public void OnAttackEnd()
    {
        playerMovement.OnAttackEnd();
    }

    public void OnBlockEnd()
    {
        playerMovement.OnBlockEnd();
    }
    
    public void OnBlockResultEnd()
    {
        playerMovement.OnBlockResultEnd();
    }

    public void OnBlockSuccessEnd()
    {
        playerMovement.OnBlockSuccessEnd();
    }
}
