using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

public enum EquipmentType
{
    Head,
    Chest,
    Legs,
    Feet,
    Gloves,
    Accessory,
    MainHand,
    OffHand
        
}

public abstract class Equipment : ItemSO
{
    public string equipmentName;
    public Rarity rarity;
    public EquipmentType equipmentType;

    public int bonusAttack;
    public int bonusDefense;
    public int bonusSpeed;
    public int bonusStamina;

    public int level;
    public float weight = 1f;

    public List<StatModifier> statModifiers = new List<StatModifier>();
}

[System.Serializable]
public class StatModifier
{
    public string statName; // Exemplo: "Attack", "Defense", "Speed"
    public float value;
}