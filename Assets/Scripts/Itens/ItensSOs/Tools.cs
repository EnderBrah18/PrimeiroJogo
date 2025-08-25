using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType
{
    None,
    Axe,
    Pickaxe,
    Hoe,
    Shovel
}



[CreateAssetMenu(fileName = "NewTool", menuName = "Equipment/Tool")]
public class Tools : Equipment
{

    public ToolType toolType;
    public GameObject prefab;

    public List<StatModifier> statusModifiers = new List<StatModifier>()
{
    new StatModifier { statName = "CollectSpeed", value = 1.5f },
    new StatModifier { statName = "ExtraYieldChance", value = 25f }, // 25% chance
    new StatModifier { statName = "ExtraYieldAmount", value = 2f }    // +2 itens
};
}


