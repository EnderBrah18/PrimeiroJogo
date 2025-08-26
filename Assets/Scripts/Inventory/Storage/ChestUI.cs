using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestUI : MonoBehaviour
{
    public GameObject uiPanel;   // painel da UI do baú
    public InventoryUI inventoryUI; // script que mostra slots (mesmo usado no inventário do player)

    private Chest currentChest;

    public void OpenChest(Chest chest)
    {
        currentChest = chest;
        uiPanel.SetActive(true);

        // Atualiza UI com o inventário do baú
        inventoryUI.Setup(currentChest.chestInventory);
    }

    public void CloseChest()
    {
        currentChest = null;
        uiPanel.SetActive(false);
    }
}
