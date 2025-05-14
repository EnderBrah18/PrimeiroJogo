using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    public GameObject toolSlotPrefab;
    public GameObject collectableSlotPrefab;

    public Transform toolContainer;
    public Transform collectableContainer;

    private List<InventorySlotUI> collectableSlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> toolSlots = new List<InventorySlotUI>();

    public GameObject resourcesTab;
    public GameObject equipmentTab;
    public GameObject menuTab;

    public Button resourcesButton;
    public Button equipmentButton;
    public Button menuButton;

    public int minSlotCount = 20;
    public GameObject itemDetailsPanel;
    public Image itemIcon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;
    public TextMeshProUGUI itemStats;

    private List<GameObject> toolSlotObjects = new();
    private List<GameObject> collectableSlotObjects = new();

    private void Awake()
    {
        // Preencher a lista de slots já existentes na cena
        foreach (Transform child in collectableContainer)
        {
            var slot = child.GetComponent<InventorySlotUI>();
            if (slot != null)
                collectableSlots.Add(slot);
        }

        foreach (Transform child in toolContainer)
        {
            var slot = child.GetComponent<InventorySlotUI>();
            if (slot != null)
                toolSlots.Add(slot);
        }
    }

    void Start()
    {
        resourcesButton.onClick.AddListener(() => OpenTab("recursos"));
        equipmentButton.onClick.AddListener(() => OpenTab("equipamento"));
        menuButton.onClick.AddListener(() => OpenTab("menu"));

        OpenTab("recursos");
    }

    public void UpdateUI()
    {
        ClearUI(); // Limpa os slots antigos

        var tools = InventorySystem.Instance.toolInventory;
        var collectables = InventorySystem.Instance.collectableInventory;

        // Atualizar slots para ferramentas
        foreach (var tool in tools)
        {
            GameObject slotObj = Instantiate(toolSlotPrefab, toolContainer);
            slotObj.GetComponent<InventorySlotUI>().SetItem(tool);
            toolSlotObjects.Add(slotObj);
        }

        // Atualizar slots para itens coletáveis
        foreach (var item in collectables)
        {
            // Verificar se já existe um slot para este item
            var existingSlot = collectableSlotObjects.Find(slot => slot.GetComponent<InventorySlotUI>().GetItem()?.itemID == item.itemID);

            if (existingSlot != null)
            {
                existingSlot.GetComponent<InventorySlotUI>().SetItem(item); // Atualiza o item no slot existente
            }
            else
            {
                GameObject slotObj = Instantiate(collectableSlotPrefab, collectableContainer);
                slotObj.GetComponent<InventorySlotUI>().SetItem(item);
                collectableSlotObjects.Add(slotObj);
            }
        }
    }

    private void ClearUI()
    {
        foreach (var obj in toolSlotObjects) Destroy(obj);
        foreach (var obj in collectableSlotObjects) Destroy(obj);

        toolSlotObjects.Clear();
        collectableSlotObjects.Clear();
    }

    private void OpenTab(string tabName)
    {
        resourcesTab.SetActive(tabName == "recursos");
        equipmentTab.SetActive(tabName == "equipamento");
        menuTab.SetActive(tabName == "menu");
    }

    private void OnItemSelected(InventoryItem item)
    {
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(true);

        itemIcon.sprite = item.collectable?.GetIcon();
        itemName.text = item.itemID;
        itemDescription.text = item.collectable?.GetWeight().ToString();
        itemStats.text = $"Peso: {item.weightPerUnit}\nTotal: {item.TotalWeight}";
    }

    public void EnsureGridSize()
    {
        // Verificar quantos slots já existem
        int requiredSlots = Mathf.Max(20, InventorySystem.Instance.collectableInventory.Count); // No mínimo 20 slots

        while (collectableSlotObjects.Count < requiredSlots)
        {
            GameObject slotObj = Instantiate(collectableSlotPrefab, collectableContainer);
            collectableSlotObjects.Add(slotObj);
        }

        // Se há slots extras que não estão sendo usados, podemos destruí-los ou manter o grid maior
        if (collectableSlotObjects.Count > requiredSlots)
        {
            for (int i = collectableSlotObjects.Count - 1; i >= requiredSlots; i--)
            {
                Destroy(collectableSlotObjects[i]);
                collectableSlotObjects.RemoveAt(i);
            }
        }
    }
}
