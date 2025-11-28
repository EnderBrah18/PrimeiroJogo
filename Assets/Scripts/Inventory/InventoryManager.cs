using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CraftingStationManager;
using static DialogueUI;
using static ShopManager;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryPanel;
    public ThirdPersonCamera cameraScript; // Referência para seu script de câmera

    public InputActionReference toggleInventoryAction;

    private bool isOpen = false;
    private bool blocked = false;

    void Update()
    {
        // Exemplo: tecla I para abrir/fechar inventário
        if (toggleInventoryAction.action.WasPressedThisFrame())
        {
            if (!ShopUIManager.IsShopOpen && !CraftingUIManager.IsCraftingOpen && !DialogueUIManager.IsDialogueOpen)
            {
                ToggleInventory();
            }
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        blocked = !blocked;
        inventoryPanel.SetActive(isOpen);

        if (cameraScript != null)
        {
            cameraScript.HandleInventoryToggled(isOpen);
            Player.Instance?.SetMovementBlocked(blocked);// Libera ou bloqueia o mouse/câmera
            Player.Instance?.SetAttackBlocked(blocked);
        }
    }

}
