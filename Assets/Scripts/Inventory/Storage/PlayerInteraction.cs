// 04/10/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Inventory playerInventory;
    public float interactionRange = 3f;
    public LayerMask containerLayer;
    public InputActionReference interactAction;

    private Container currentContainer;

    void Update()
    {
        // Check if the interaction button is pressed
        if (interactAction.action.WasPressedThisFrame())
        {
            if (currentContainer != null)
            {
                // Close the container if it's already open
                currentContainer.CloseContainerUI();
                currentContainer = null;
            }
            else
            {
                // Try to open a container
                TryOpenContainer();
            }
        }

        // Check if the player has left the interaction area
        if (currentContainer != null && !IsWithinInteractionRange(currentContainer.transform.position))
        {
            currentContainer.CloseContainerUI();
            currentContainer = null;
        }
    }

    private void TryOpenContainer()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactionRange, containerLayer))
        {
            Container container = hit.collider.GetComponent<Container>();
            if (container != null)
            {
                currentContainer = container;
                container.OpenContainerUI(playerInventory);
            }
        }
    }

    private bool IsWithinInteractionRange(Vector3 targetPosition)
    {
        // Check if the player is still within the interaction range of the container
        return Vector3.Distance(transform.position, targetPosition) <= interactionRange;
    }
}
