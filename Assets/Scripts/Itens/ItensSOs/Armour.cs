using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ArmourType
{
    Helment,
    Chestplate,
    Leggings,
    Boots,
    Shield
}

[CreateAssetMenu(fileName = "NewArmour", menuName = "Equipment/Armour")]
public class Armour : Equipment
{
    public ArmourType armourType; // Enum opcional: Helmet, Chestplate, etc.



}
