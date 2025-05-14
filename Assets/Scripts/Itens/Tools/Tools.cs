using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    None,
    Axe,
    Pickaxe,
    Hoe
}

[CreateAssetMenu(fileName = "NewTool", menuName = "Tool")]
public class Tools : ScriptableObject
{
    public string toolName;
    public ToolType toolType;
    public int level;
    public Sprite icon;
    public GameObject prefab;

    public float weight = 1f;
}
