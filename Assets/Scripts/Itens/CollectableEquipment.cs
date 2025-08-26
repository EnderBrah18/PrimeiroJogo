using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableEquipment : CollectableObject
{
    public Equipment equipmentData; // Referência ao ScriptableObject do equipamento

    private Inventory playerInventory;

    private void Reset()
    {
        if (equipmentData != null)
        {
            itemName = equipmentData.equipmentName;
            icon = equipmentData.icon;
            description = equipmentData.description;
            weight = equipmentData.weight;
        }
    }

    private void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>().inventory;
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

    public override string GetID() => equipmentData != null ? equipmentData.equipmentName : itemName;
    public override Sprite GetIcon() => equipmentData != null ? equipmentData.icon : icon;
    public override string GetDescription() => equipmentData != null ? equipmentData.description : description;
    public override float GetWeight() => equipmentData != null ? equipmentData.weight : weight;
}
