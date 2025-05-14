using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryToogle : MonoBehaviour
{

    public GameObject inventoryUI;
    public bool pauseTime = true;

    private bool isInventoryOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);

        // Libera ou trava o mouse
        Cursor.visible = isInventoryOpen;
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;

        // Pausa ou retoma o tempo
        Time.timeScale = (isInventoryOpen && pauseTime) ? 0f : 1f;

        // Notifique outros sistemas se necessário (ex: PlayerMovement, CameraControl etc.)
    }

    public bool IsInventoryOpen() => isInventoryOpen;
}
