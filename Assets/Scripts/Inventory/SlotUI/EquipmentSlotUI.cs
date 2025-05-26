using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class EquipmentSlotUI : InventorySlotUI, IPointerClickHandler
{
    public TextMeshProUGUI slotName;

    public static System.Action<Equipment> OnEquipmentSelected;

    public Equipment currentEquipment;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if (currentItem != null && currentItem.IsEquipment())
        {
            Equipment equipment = currentItem.equipment;
            OnEquipmentSelected?.Invoke(equipment);
        }
    }

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
