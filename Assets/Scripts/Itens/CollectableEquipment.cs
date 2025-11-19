using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableEquipment : CollectableObject
{
    public Equipment equipmentData; // Referência ao ScriptableObject do equipamento

    private Inventory playerInventory;

    private InventoryUI inventoryUI;

    

    private void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>().inventory;
        inventoryUI = FindFirstObjectByType<InventoryUI>();
    }

    public override void StartCollect(Tools equippedTool)
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("Nenhum equipamento atribuído ao coletável!");
            return;
        }

        if (playerInventory != null && equipmentData != null)
        {
            // Pass 'true' for the isEquipment parameter
            bool added = playerInventory.AddItem(equipmentData, 1);
            if (added)
            {
                Debug.Log($"{equipmentData.equipmentName} coletado e adicionado ao inventário!");
            }
            else
            {
                Debug.Log("Inventário cheio ou peso excedido!");
                // Aqui você pode decidir: descartar no chão, criar um drop físico, etc.
            }
        }

        Destroy(gameObject); // Remove o objeto do mundo após coleta
    }

    public override bool CanBeCollected(Tools currentTool)
    {
        // Equipamentos podem ser coletados sem ferramenta, ou você define outra lógica
        return true;
    }

}
