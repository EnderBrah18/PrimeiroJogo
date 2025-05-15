using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel; // O painel com seus slots
    public ItemInfoPanel itemInfoPanel;
    public GameObject resourcePanel;
    public GameObject equipmentPanel;
    private bool isInventoryOpen = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        inventoryPanel.SetActive(isInventoryOpen);

        

        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None; // Libera o cursor
            Cursor.visible = true;
            Time.timeScale = 0f; // Pausa o jogo
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Trava o cursor no centro
            Cursor.visible = false;
            Time.timeScale = 1f; // Retoma o jogo
        }
    }

    public void OpenResourceTab()
    {
        resourcePanel.SetActive(true);
        equipmentPanel.SetActive(false);
    }

    public void OpenEquipmentTab()
    {
        resourcePanel.SetActive(false);
        equipmentPanel.SetActive(true);
    }
}
