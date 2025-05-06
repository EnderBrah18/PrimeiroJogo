using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public string itemName = "Flor";

    public void Collect()
    {
        Debug.Log($"Você coletou: {itemName}");
        Destroy(gameObject); // remove o item do mundo
    }
}
