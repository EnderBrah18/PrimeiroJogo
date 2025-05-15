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

        // Apenas adiciona os slots que já existem no editor, se houver
        foreach (Transform child in parent)
        {
            var slot = child.GetComponent<InventorySlot>();
            if (slot != null)
                slotList.Add(slot);
        }
    }

    public void RefreshUI()
    {
        FillSlots(toolSlots, InventorySystem.Instance.toolInventory, toolGridParent);
        FillSlots(collectableSlots, InventorySystem.Instance.collectableInventory, collectableGridParent);
    }

    // Preenche os slots já existentes com os itens e adiciona slots extras se necessário
    void FillSlots(List<InventorySlot> slots, List<InventoryItem> items, Transform parentTransform)
    {
        // Cria slots até ter o suficiente
        while (slots.Count < items.Count)
        {
            GameObject slotGO = Instantiate(slotPrefab, parentTransform);
            slotGO.name = "Slot " + slots.Count;
            var slot = slotGO.GetComponent<InventorySlot>();
            slots.Add(slot);
        }

        // Preenche os slots com itens
        for (int i = 0; i < items.Count; i++)
        {
            slots[i].SetItem(items[i]);
            slots[i].gameObject.SetActive(true);
        }

        // Limpa e desativa slots extras
        for (int i = items.Count; i < slots.Count; i++)
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
