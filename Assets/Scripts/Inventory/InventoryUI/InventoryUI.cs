using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    public GameObject slotPrefab;
    public Transform toolGridParent;
    public Transform collectableGridParent;

    public GameObject toolTab;
    public GameObject collectableTab;

    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public Image itemIcon;

    private List<InventorySlot> toolSlots = new List<InventorySlot>();
    private List<InventorySlot> collectableSlots = new List<InventorySlot>();

    private int baseSlotCount = 20;

    private void Awake()
    {
        Instance = this;

        InitializeSlots(toolGridParent, toolSlots);
        InitializeSlots(collectableGridParent, collectableSlots);
    }

    // Inicializa os slots fixos no grid e armazena as referências
    void InitializeSlots(Transform parent, List<InventorySlot> slotList)
    {
        slotList.Clear();

        // Verifica se já existem filhos no grid (slots pré-definidos no editor)
        int existingSlots = parent.childCount;

        // Se tem menos que baseSlotCount, cria slots para completar
        for (int i = existingSlots; i < baseSlotCount; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, parent);
            slotGO.name = "Slot " + i;
        }

        // Adiciona todos os filhos (slots) à lista para fácil acesso
        foreach (Transform child in parent)
        {
            var slot = child.GetComponent<InventorySlot>();
            if (slot != null)
                slotList.Add(slot);
        }
    }

    public void RefreshUI()
    {
        // Preenche os slots de ferramentas
        FillSlots(toolSlots, InventorySystem.Instance.toolInventory);

        // Preenche os slots de coletáveis
        FillSlots(collectableSlots, InventorySystem.Instance.collectableInventory);
    }

    // Preenche os slots já existentes com os itens e adiciona slots extras se necessário
    void FillSlots(List<InventorySlot> slots, List<InventoryItem> items)
    {
        int i = 0;
        // Preenche slots existentes
        for (; i < slots.Count && i < items.Count; i++)
        {
            slots[i].SetItem(items[i]);
            slots[i].gameObject.SetActive(true);
        }

        // Se ainda tiver itens para mostrar, cria mais slots dinamicamente
        for (; i < items.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slots[0].transform.parent);
            slotGO.name = "Slot " + i;
            var slot = slotGO.GetComponent<InventorySlot>();
            slot.SetItem(items[i]);
            slots.Add(slot);
        }

        // Desativa slots excedentes (quando tiver menos itens que slots)
        for (; i < slots.Count; i++)
        {
            slots[i].ClearSlot();
            slots[i].gameObject.SetActive(false);
        }
    }

    public void ShowItemDetails(InventoryItem item)
    {
        if (item == null)
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            return;
        }

        itemNameText.text = item.IsTool() ? item.tool.toolName : item.collectable.GetID();
        itemDescriptionText.text = item.IsTool() ? item.tool.description : item.collectable.GetDescription();
        itemIcon.sprite = item.IsTool() ? item.tool.icon : (item.collectable as CollectableObject)?.icon;
        itemIcon.enabled = true;
    }

    public void OpenToolTab()
    {
        toolTab.SetActive(true);
        collectableTab.SetActive(false);
    }

    public void OpenCollectableTab()
    {
        toolTab.SetActive(false);
        collectableTab.SetActive(true);
    }
}
