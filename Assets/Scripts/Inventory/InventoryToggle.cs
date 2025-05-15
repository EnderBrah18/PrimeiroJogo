using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel; // O painel com seus slots
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
}
