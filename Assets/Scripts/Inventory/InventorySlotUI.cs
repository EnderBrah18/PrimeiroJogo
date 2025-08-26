using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Dados do slot")]
    public InventorySlot slotData;          // referência ao slot real
    public Image icon;                      // ícone do item
    public TextMeshProUGUI amountText;      // quantidade do item

    [Header("Prefabs")]
    public GameObject placeholderPrefab;    // prefab simplificado só com moldura/fundo

    private CanvasGroup canvasGroup;
    private GameObject placeholder;         // objeto que mantém o espaço
    private GameObject dragIcon;            // ícone que segue o mouse
    private Transform parentAfterDrag;      // parent original do slot


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(InventorySlot data)
    {
        slotData = data;
        icon.sprite = data.item.icon;
        amountText.text = data.quantity > 1 ? data.quantity.ToString() : "";
    }

    // Quando começa a arrastar
    public void OnBeginDrag(PointerEventData eventData)
    {

        parentAfterDrag = transform.parent;

        // Criar placeholder simples para manter espaço na grade
        placeholder = Instantiate(placeholderPrefab);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Criar ícone que segue o mouse
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(transform.root);
        dragIcon.transform.SetAsLastSibling();

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false;

        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).sizeDelta;

        // Ocultar ícone original (mantendo o slot/fundo se quiser)
        icon.enabled = false;
        amountText.enabled = false;

        canvasGroup.blocksRaycasts = false;
    }

    // Enquanto arrasta
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    // Quando solta
    public void OnEndDrag(PointerEventData eventData)
    {
        // Voltar slot para a posição original
        transform.SetParent(parentAfterDrag);
        transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());

        // Reativar ícone e quantidade
        icon.enabled = true;
        amountText.enabled = true;

        canvasGroup.blocksRaycasts = true;

        // Destruir dragIcon e placeholder
        Destroy(dragIcon);
        Destroy(placeholder);
    }

    // Quando solta em outro slot válido
    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI otherSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (otherSlotUI != null && otherSlotUI != this)
        {
            // Troca os itens entre os slots
            InventorySlot temp = slotData;
            slotData = otherSlotUI.slotData;
            otherSlotUI.slotData = temp;

            // Atualiza visual
            Setup(slotData);
            otherSlotUI.Setup(otherSlotUI.slotData);
        }
    }
}
