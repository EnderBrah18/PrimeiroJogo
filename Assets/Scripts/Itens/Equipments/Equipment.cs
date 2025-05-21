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

public abstract class Equipment : ScriptableObject
{
    public string equipmentName;
    public Rarity rarity;
    public int level;
    [TextArea] public string description;
    public Sprite icon;
    public float weight = 1f;

    public List<StatModifier> statModifiers = new List<StatModifier>();
}

[System.Serializable]
public class StatModifier
{
    public string statName; // Exemplo: "Attack", "Defense", "Speed"
    public float value;
}