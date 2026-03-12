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
}
