using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class ItemInfoPanel : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI weightText;

   

    private void OnEnable()
    {
        InventorySlotUI.OnItemSelected += ShowItemInfo;
    }

    private void OnDisable()
    {
        InventorySlotUI.OnItemSelected -= ShowItemInfo;
    }

    private void ShowItemInfo(InventoryItem item)
    {
        gameObject.SetActive(true);

        icon.sprite = item.GetIcon();
        nameText.text = item.GetID();
        descriptionText.text = item.GetDescription();

        float pesoUnitario = item.GetWeight();
        int quantidade = item.quantity;
        float pesoTotal = pesoUnitario * quantidade;

        if (quantidade > 1)
        {
            weightText.text = $"Peso: {pesoUnitario} kg (unidade) | {pesoTotal} kg (total)";
        }
        else
        {
            weightText.text = $"Peso: {pesoUnitario} kg";
        }
    }

    

}
