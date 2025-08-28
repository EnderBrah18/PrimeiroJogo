using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestUI : MonoBehaviour
{
    public InventoryManager inventoryManager; // Reference to InventoryManager
    public GameObject chestCanvas;
    public InventoryUI chestInventoryUI;
    public InventoryUI playerInventoryUI;

    private Chest currentChest;

    public void OpenChest(Chest chest, Inventory playerInventory)
    {
        currentChest = chest;
        chestCanvas.SetActive(true);
        inventoryManager.ToggleInventory();

        // Configure chest inventory UI
        if (chest.chestInventory == null)
        {
            Debug.LogError("Chest inventory is null!");
            return;
        }

        // Configura inventário do baú
        chestInventoryUI.Setup(chest.chestInventory);
        chestInventoryUI.UpdateChestUI(); // Mostra os slots genéricos do baú

        // Configura inventário do jogador
        playerInventoryUI.Setup(playerInventory);
        playerInventoryUI.ShowRecursos(); // Aqui faz sentido manter separado

        Debug.Log("Chest opened!");
    }

    public void CloseChest()
    {
        currentChest = null;
        chestCanvas.SetActive(false);
        inventoryManager.ToggleInventory();
    }
}
