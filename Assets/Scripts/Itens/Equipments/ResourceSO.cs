using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Inventory/Resource")]
public class ResourceSO : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
    public string description;
    public float weight;
    public ToolType requiredToolType;
    public int requiredToolLevel;
    public float baseCollectTime;
    public CollectableType type;
}
