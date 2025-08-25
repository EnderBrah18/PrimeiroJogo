using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel; // O painel com seus slots
    public ItemInfoPanel itemInfoPanel;
    public GameObject resourcePanel;
    public GameObject equipmentPanel;
    public InputActionReference openInventory;
    private bool isInventoryOpen = false;

    public static event System.Action<bool> OnInventoryToggled;

    private void Update()
    {
        if (openInventory.action.WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);

        OnInventoryToggled?.Invoke(isInventoryOpen); // dispara evento

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
