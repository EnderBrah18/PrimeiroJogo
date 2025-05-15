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

    public Tools tool; // Referência à ferramenta, se for uma ferramenta
    public ICollectable collectable; // Referência ao item coletável, se for um coletável

    // Construtores para diferentes tipos de itens
    public InventoryItem(Tools tool)
    {
        itemID = tool.toolName;
        this.tool = tool;
        quantity = 1;
    }

    public InventoryItem(ICollectable collectable)
    {
        itemID = collectable.GetID();
        this.collectable = collectable;
        quantity = 1;
        weightPerUnit = (collectable as CollectableObject)?.weight ?? 0f;
    }

    public float TotalWeight => weightPerUnit * quantity;

    public string GetID() => collectable.GetID();
    public Sprite GetIcon() => collectable.GetIcon(); // supondo que tenha

    public bool IsTool() => tool != null;
    public bool IsCollectable() => collectable != null;
}
