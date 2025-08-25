using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Inventory/Resource")]
public class ResourceSO : ItemSO
{
    public string resourceName;
    public float weight;
    public ToolType requiredToolType;
    public int requiredToolLevel;
    public float baseCollectTime;
    public CollectableType type;
}
