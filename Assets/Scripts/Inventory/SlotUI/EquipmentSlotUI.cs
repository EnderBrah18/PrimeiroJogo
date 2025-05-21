using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class EquipmentSlotUI : InventorySlotUI
{
    public TextMeshProUGUI slotName;
    public Image rarityBorder;

    public static event System.Action<Equipment> OnEquipmentSelected;
    public override void RefreshSlotUI()
    {
        base.RefreshSlotUI();

        if (currentItem != null && currentItem.IsEquipment())
        {
            Equipment eq = currentItem.equipment;
            slotName.text = eq.equipmentName;
            rarityBorder.color = GetRarityColor(eq.rarity);

            // Emitir o evento para mostrar informações no painel
            OnEquipmentSelected?.Invoke(eq);
        }
        else
        {
            slotName.text = "";
            rarityBorder.color = Color.clear;
        }
    }

    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => Color.gray,
            Rarity.Uncommon => Color.green,
            Rarity.Rare => Color.blue,
            Rarity.Epic => new Color(0.6f, 0f, 1f),       // Roxo
            Rarity.Legendary => new Color(1f, 0.5f, 0f),  // Laranja Dourado
            Rarity.Mythic => Color.red,
            _ => Color.white
        };
    }
}
