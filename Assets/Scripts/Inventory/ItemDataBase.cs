using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Database")]
public class ItemDatabase : ScriptableObject
{
    public List<Tools> allTools;

    public Tools GetToolByID(string id)
    {
        return allTools.Find(t => t.toolName == id);
    }
}
