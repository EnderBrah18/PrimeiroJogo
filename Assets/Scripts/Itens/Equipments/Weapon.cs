using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Sword,
    Bow
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Equipment/Weapon")]
public class Weapon : Equipment
{
    public GameObject modelPrefab;
    public float baseDamage;
    public float attackSpeed;
}
