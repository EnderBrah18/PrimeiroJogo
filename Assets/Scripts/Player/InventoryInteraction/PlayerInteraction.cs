using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public ChestUI chestUI;
    public InputActionReference interactAction;
    public PlayerInventory playerInventory;

    private void Update()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory is not assigned in PlayerInteraction.");
            return;
        }

        if (interactAction.action.WasPressedThisFrame())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 3f))
            {
                Chest chest = hit.collider.GetComponent<Chest>();
                if (chest != null)
                {
                    chestUI.OpenChest(chest, playerInventory.inventory);
                }
            }
        }
    }
}
