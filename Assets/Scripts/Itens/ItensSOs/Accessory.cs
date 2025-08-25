using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AcessoryType
{
    Ring,
    Necklace,
    Bracelet
}

[CreateAssetMenu(fileName = "NewRune", menuName = "Equipment/Rune")]
public class Accessory : Equipment
{
    public string effectDescription;
    public List<string> compatibleItemTypes; // Ex: "Weapon", "Armour"
}
