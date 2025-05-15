using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Database")]
public class ItemDatabase : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int weight;
    public int quantity;
}

