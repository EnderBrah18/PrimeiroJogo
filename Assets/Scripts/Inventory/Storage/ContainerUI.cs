using System;
using UnityEditor;
using UnityEngine;

public class ContainerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject containerUICanvas; // Reference to the Canvas GameObject
    public InventoryUI containerInventoryUI; // UI for the container's inventory
    public InventoryUI playerInventoryUI; // UI for the player's inventory

    private Container container;
    private Inventory playerInventory;
    public InventoryManager inventoryManager;

    private void Awake()
    {
        // Ensure the Canvas is inactive at the start
        if (containerUICanvas != null)
        {
            containerUICanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("ContainerUICanvas is not assigned in ContainerUI.");
        }
    }

    /// <summary>
    /// Sets up the container UI with the container and player inventory.
    /// </summary>
    /// <param name="container">The container being interacted with.</param>
    /// <param name="playerInventory">The player's inventory.</param>
    public void Setup(Container container, Inventory playerInventory)
    {
        this.container = container;
        this.playerInventory = playerInventory;

        if (containerUICanvas == null)
        {
            Debug.LogError("ContainerUICanvas is not assigned in ContainerUI.");
            return;
        }

        // Ativa o Canvas do container
        containerUICanvas.SetActive(true);

        // Ativa também o inventário do jogador se não estiver aberto
        if (inventoryManager != null && !inventoryManager.inventoryPanel.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }

        // Setup do container inventory UI
        if (containerInventoryUI != null)
        {
            containerInventoryUI.Setup(container.containerInventory);
            containerInventoryUI.UpdateChestUI();
        }

        // Setup do player inventory UI
        if (playerInventoryUI != null)
        {
            playerInventoryUI.Setup(playerInventory);
            playerInventoryUI.ShowRecursos();
        }
    }

    /// <summary>
    /// Closes the container UI.
    /// </summary>
    public void CloseUI()
    {
        if (containerUICanvas != null)
        {
            containerUICanvas.SetActive(false);
        }

        // Fecha o inventário do jogador se estiver aberto
        if (inventoryManager != null && inventoryManager.inventoryPanel.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }
    }
}
