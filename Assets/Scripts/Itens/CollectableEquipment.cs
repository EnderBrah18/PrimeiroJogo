using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableEquipment : CollectableObject
{
    public Equipment equipmentData; // Referência ao ScriptableObject do equipamento

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

    public override void StartCollect(Tools equippedTool)
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("Nenhum equipamento atribuído ao coletável!");
            return;
        }

        float itemWeight = GetWeight();
        if (InventorySystem.Instance.currentWeight + itemWeight > InventorySystem.Instance.maxWeight)
        {
            Debug.Log("Inventário cheio. Não é possível coletar o equipamento.");
            return;
        }

        InventorySystem.Instance.AddEquipment(equipmentData);
        Debug.Log($"Você coletou o equipamento: {equipmentData.equipmentName}");

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
