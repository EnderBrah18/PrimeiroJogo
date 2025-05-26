using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerEquipmentSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [Header("Tipo deste slot de equipamento")]
    public EquipmentType slotType;

    [Header("Referência ao jogador")]
    public Player player;

    [Header("UI")]
    public TextMeshProUGUI slotText;

    private Equipment equippedItem;
    private GameObject dragIcon;

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (draggedSlot == null || !draggedSlot.HasItem()) return;

        InventoryItem item = draggedSlot.GetItem();
        if (!item.IsEquipment()) return;

        Equipment equipment = item.equipment;
        if (equipment.equipmentType != slotType) return;

        Equip(equipment);
        draggedSlot.Clear();
    }

    public void Equip(Equipment equipment)
    {
        // Se já há um item equipado, devolve-o ao inventário antes
        if (equippedItem != null)
        {
            InventorySystem.Instance.AddEquipment(equippedItem);
        }

        equippedItem = equipment;
        slotText.text = equipment.equipmentName;

        // Exibe as informações do item
        EquipmentSlotUI.OnEquipmentSelected?.Invoke(equipment);

        // Atualiza o equipamento do jogador
        switch (slotType)
        {
            case EquipmentType.Head: player.helmet = equipment; break;
            case EquipmentType.Chest: player.chest = equipment; break;
            case EquipmentType.Legs: player.legs = equipment; break;
            case EquipmentType.Feet: player.boots = equipment; break;
            case EquipmentType.Gloves: player.gloves = equipment; break;
            case EquipmentType.Accessory: player.accessory = equipment; break;
            case EquipmentType.MainHand: player.mainHand = equipment; break;
            case EquipmentType.OffHand: player.offHand = equipment; break;
        }

        Debug.Log($"Equipado: {equipment.equipmentName} no slot {slotType}");
    }

    public void ClearSlot()
    {
        equippedItem = null;
        slotText.text = slotType.ToString();
        switch (slotType)
        {
            case EquipmentType.Head: player.helmet = null; break;
            case EquipmentType.Chest: player.chest = null; break;
            case EquipmentType.Legs: player.legs = null; break;
            case EquipmentType.Feet: player.boots = null; break;
            case EquipmentType.Gloves: player.gloves = null; break;
            case EquipmentType.Accessory: player.accessory = null; break;
            case EquipmentType.MainHand: player.mainHand = null; break;
            case EquipmentType.OffHand: player.offHand = null; break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (equippedItem == null) return;

        // Cria ícone visual
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(transform.root, false);
        Image image = dragIcon.AddComponent<Image>();
        image.raycastTarget = false;

        // Ícone temporário (pode ser substituído por um sprite do item)
        image.color = Color.yellow;
        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 50);

        dragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            Destroy(dragIcon);

        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        if (dropTarget == null || dropTarget.GetComponent<PlayerEquipmentSlotUI>() != this)
        {
            // Desequipar e mandar pro inventário
            if (equippedItem != null)
            {
                InventorySystem.Instance.AddEquipment(equippedItem); // Assumindo que você tenha um sistema singleton
                ClearSlot();
            }
        }
    }
}
