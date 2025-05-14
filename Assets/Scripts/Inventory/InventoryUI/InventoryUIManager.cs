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
            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.SetItem(tool);

            // Passa a referência para este script (InventoryUI)
            slotUI.SetInventoryUI(this);

            toolSlotObjects.Add(slotObj);
        }

        // Atualizar slots para itens coletáveis
        foreach (var item in collectables)
        {
            var existingSlot = collectableSlotObjects.Find(slot => slot.GetComponent<InventorySlotUI>().GetItem()?.itemID == item.itemID);

            if (existingSlot != null)
            {
                existingSlot.GetComponent<InventorySlotUI>().SetItem(item);
            }
            else
            {
                GameObject slotObj = Instantiate(collectableSlotPrefab, collectableContainer);
                var slotUI = slotObj.GetComponent<InventorySlotUI>();
                slotUI.SetItem(item);

                // Passa a referência para este script (InventoryUI)
                slotUI.SetInventoryUI(this);

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

    public void OnItemSelected(InventoryItem item)
    {
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(true);

        itemIcon.sprite = item.collectable?.GetIcon();
        itemName.text = item.itemID;
        itemDescription.text = item.collectable?.GetWeight().ToString();
        itemStats.text = $"Peso: {item.weightPerUnit}\nTotal: {item.TotalWeight}";
    }

    

}
