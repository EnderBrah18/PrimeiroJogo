using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryWeighUI : MonoBehaviour
{
    public TextMeshProUGUI weightText;

    // Supondo que você tenha um Inventário global
    public InventorySystem playerInventory;

    private void Update()
    {
        float currentWeight = playerInventory.currentWeight;
        float maxWeight = playerInventory.maxWeight;

        weightText.text = $"Peso: {currentWeight} / {maxWeight} kg";
    }
}
