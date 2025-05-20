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
            RestaurarTodosFilhosDoInventario(); // <-- Aqui
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
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

    private void RestaurarTodosFilhosDoInventario()
    {
        foreach (Transform filho in resourcePanel.transform)
        {
            filho.gameObject.SetActive(true);
        }

        foreach (Transform filho in equipmentPanel.transform)
        {
            filho.gameObject.SetActive(true);
        }
    }
}
