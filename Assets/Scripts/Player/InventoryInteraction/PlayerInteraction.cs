using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public ChestUI chestUI;
    public InputActionReference interactAction;

    private void Update()
    {
        if (interactAction.action.WasPressedThisFrame())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 3f))
            {
                Chest chest = hit.collider.GetComponent<Chest>();
                if (chest != null)
                {
                    chestUI.OpenChest(chest);
                }
            }
        }
    }
}
