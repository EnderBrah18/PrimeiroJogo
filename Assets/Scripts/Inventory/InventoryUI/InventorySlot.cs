using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    private InventoryItem currentItem;

    // Para drag & drop
    private Canvas canvas;
    private RectTransform dragIcon;
    private Image dragIconImage;
    private Transform originalParent;

    private static InventorySlot draggedSlot; // slot que está sendo arrastado

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        // Criar objeto para ícone arrastável (invisível inicialmente)
        GameObject dragGO = new GameObject("DragIcon");
        dragGO.transform.SetParent(canvas.transform);
        dragIcon = dragGO.AddComponent<RectTransform>();
        dragIcon.sizeDelta = new Vector2(40, 40);
        dragIcon.gameObject.SetActive(false);

        dragIconImage = dragGO.AddComponent<Image>();
        dragIconImage.raycastTarget = false;
    }

    public void SetItem(InventoryItem item)
    {
        currentItem = item;
        if (item == null || item.quantity <= 0)
        {
            ClearSlot();
            return;
        }

        iconImage.sprite = item.IsTool() ? item.tool.icon : (item.collectable as CollectableObject)?.icon;
        iconImage.enabled = true;

        quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
        quantityText.enabled = item.quantity > 1;
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
        iconImage.enabled = false;
        quantityText.text = "";
        quantityText.enabled = false;
    }

    // Drag & Drop interface

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        draggedSlot = this;

        dragIcon.gameObject.SetActive(true);
        dragIcon.position = eventData.position;
        dragIconImage.sprite = iconImage.sprite;

        originalParent = transform.parent;
        iconImage.enabled = false;
        quantityText.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot != this) return;

        dragIcon.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot != this) return;

        dragIcon.gameObject.SetActive(false);

        iconImage.enabled = true;
        quantityText.enabled = currentItem != null && currentItem.quantity > 1;

        // Raycast para pegar todos os objetos UI abaixo do ponteiro
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        InventorySlot targetSlot = null;

        foreach (var res in results)
        {
            // Ignora o slot que está sendo arrastado (o original)
            if (res.gameObject == gameObject) continue;

            // Tenta encontrar InventorySlot no objeto ou em seus pais
            targetSlot = res.gameObject.GetComponentInParent<InventorySlot>();
            if (targetSlot != null && targetSlot != this)
            {
                break; // Encontrou um slot diferente
            }
        }

        if (targetSlot != null)
        {
            // Faz a troca dos itens
            SwapItems(targetSlot);
        }
        else
        {
            // Não soltou em slot válido, pode implementar descartar aqui ou voltar para original
            // Por enquanto, volta ao original (já feito ao reativar o ícone)
        }

        draggedSlot = null;
    }

    // Trocar os itens entre esse slot e o target
    private void SwapItems(InventorySlot target)
    {
        var temp = target.currentItem;
        target.SetItem(currentItem);
        SetItem(temp);

        // Aqui você deve sincronizar o inventário real (InventorySystem) também,
        // para garantir que a troca afete os dados

        InventorySystem.Instance.SwapInventoryItems(this.currentItem, target.currentItem);
        InventoryUI.Instance.ShowItemDetails(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Clique direito para usar/equipar (implementar depois)
        if (eventData.button == PointerEventData.InputButton.Right && currentItem != null)
        {
            // Exemplo:
            // Use ou equipe o item
        }

        InventoryUI.Instance.ShowItemDetails(currentItem);
    }

    // Getter para o item atual
    public InventoryItem GetItem()
    {
        return currentItem;
    }
}
