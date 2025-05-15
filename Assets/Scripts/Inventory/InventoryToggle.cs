using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI; // Referência ao painel do inventário
    private bool isInventoryOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isInventoryOpen)
                CloseInventory();
            else
                OpenInventory();
        }
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        inventoryUI.SetActive(true);

        // Atualiza a interface
        InventoryUI.Instance.RefreshUI();

        // Pausa o jogo e libera o mouse
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryUI.SetActive(false);

        // Volta ao jogo
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
