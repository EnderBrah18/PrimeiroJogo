using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemID;
    public int quantity;
    public float weightPerUnit;
    public float ItemTotalWeight => weightPerUnit * quantity;

    public Equipment equipment; // Referência à ferramenta, se for uma ferramenta
    public ICollectable collectable; // Referência ao item coletável, se for um coletável

    // Construtores para diferentes tipos de itens
    public InventoryItem(Equipment equipment)
    {
        itemID = equipment.equipmentName;
        this.equipment = equipment;
        quantity = 1;
        weightPerUnit = equipment.weight;  // se Equipment tiver weight
    }

    public InventoryItem(ICollectable collectable)
    {
        itemID = collectable.GetID();
        this.collectable = collectable;
        quantity = 1;
        weightPerUnit = (collectable as CollectableObject)?.weight ?? 0f;
    }

    public float TotalWeight => weightPerUnit * quantity;

    public float GetWeight()
    {
        if (IsEquipment())
            return weightPerUnit;
        else if (IsCollectable())
            return collectable.GetWeight();
        else
            return 0f;
    }

    public string GetDescription()
    {
        if (IsEquipment())
            return equipment.description;  // supondo que Equipment tenha descrição
        else if (IsCollectable())
            return collectable.GetDescription();
        else
            return "";
    }

    public string GetID()
    {
        if (IsEquipment())
            return equipment.equipmentName;
        else if (IsCollectable())
            return collectable.GetID();
        else
            return "";
    }

    public Sprite GetIcon()
    {
        if (IsEquipment())
            return equipment.icon;
        else if (IsCollectable())
            return collectable.GetIcon();
        else
            return null;
    }

    public bool IsEquipment() => equipment != null;
    public bool IsCollectable() => collectable != null;
}
