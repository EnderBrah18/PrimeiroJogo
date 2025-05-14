using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotDrop : MonoBehaviour, IDropHandler
{
    public InventorySlotUI slotUI;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = InventoryDragHandler.draggedItem;
        if (dragged == null || dragged == slotUI) return;

        var fromSlot = dragged.GetComponent<InventorySlotUI>();
        var toSlot = slotUI;

        if (fromSlot == null || toSlot == null) return;

        var fromItem = fromSlot.GetItem();
        var toItem = toSlot.GetItem();

        // Identifica o tipo de inventário
        var inventory = fromSlot.isToolSlot
            ? InventorySystem.Instance.toolInventory
            : InventorySystem.Instance.collectableInventory;

        // Se os tipos forem diferentes (ex: um for tool e outro for coletável), não troca
        if (fromSlot.isToolSlot != toSlot.isToolSlot) return;

        int fromIndex = inventory.IndexOf(fromItem);
        int toIndex = inventory.IndexOf(toItem);

        // Evita erros se algum índice for inválido
        if (fromIndex < 0 || toIndex < 0) return;

        // Troca
        var temp = inventory[fromIndex];
        inventory[fromIndex] = inventory[toIndex];
        inventory[toIndex] = temp;

        // Atualiza UI
        FindObjectOfType<InventoryUIManager>().UpdateUI();
    }
}
