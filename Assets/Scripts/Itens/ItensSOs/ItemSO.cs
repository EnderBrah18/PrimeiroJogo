using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int amount;
    [TextArea]
    public string description;
    public ItemType itemType;
    public float Weight;

    // Você pode adicionar propriedades comuns aqui
}
