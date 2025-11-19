using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewRune", menuName = "Equipment/Rune")]
public class Rune : Equipment
{
    public string effectDescription;
    public List<string> compatibleItemTypes; // Ex: "Weapon", "Armour"
}
