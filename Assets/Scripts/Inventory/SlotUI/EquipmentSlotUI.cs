using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class EquipmentSlotUI : InventorySlotUI
{
    public TextMeshProUGUI slotName;

    public static event System.Action<Equipment> OnEquipmentSelected;
    public override void RefreshSlotUI()
    {
        base.RefreshSlotUI();

        if (currentItem != null && currentItem.IsEquipment())
        {
            Equipment eq = currentItem.equipment;
            slotName.text = eq.equipmentName;

            // Emitir o evento para mostrar informações no painel
            OnEquipmentSelected?.Invoke(eq);
        }
        else
        {
            slotName.text = "";
        }
    }

}
