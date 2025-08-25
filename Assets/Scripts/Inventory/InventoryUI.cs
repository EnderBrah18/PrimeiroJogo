using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;

    public GameObject slotPrefab;
    public Transform recursosSlotsParent;
    public Transform equipamentosSlotsParent;

    private List<GameObject> recursosSlots = new List<GameObject>();
    private List<GameObject> equipamentosSlots = new List<GameObject>();

    public GameObject recursosPanel;
    public GameObject equipamentosPanel;

    void Start()
    {
        ShowRecursos(); // Começa mostrando recursos
    }

    public void ShowRecursos()
    {
        Debug.Log("Recursos painel aberto!");
        recursosPanel.SetActive(true);
        equipamentosPanel.SetActive(false);
        UpdateRecursosUI();
    }

    public void ShowEquipamentos()
    {
        Debug.Log("Equipamentos painel aberto!");
        recursosPanel.SetActive(false);
        equipamentosPanel.SetActive(true);
        UpdateEquipamentosUI();
    }

    void UpdateRecursosUI()
    {
        foreach (var slot in recursosSlots) Destroy(slot);
        recursosSlots.Clear();

        foreach (InventorySlot invSlot in inventory.slots)
        {
            if (invSlot.item.itemType != ItemType.Resource) continue;

            GameObject slotGO = Instantiate(slotPrefab, recursosSlotsParent);
            slotGO.transform.Find("Icon").GetComponent<Image>().sprite = invSlot.item.icon;
            slotGO.transform.Find("Amount").GetComponent<TextMeshProUGUI>().text = invSlot.quantity.ToString();

            recursosSlots.Add(slotGO);
        }
    }

    void UpdateEquipamentosUI()
    {
        foreach (var slot in equipamentosSlots) Destroy(slot);
        equipamentosSlots.Clear();

        foreach (InventorySlot invSlot in inventory.slots)
        {
            if (invSlot.item.itemType != ItemType.Equipment) continue;

            GameObject slotGO = Instantiate(slotPrefab, equipamentosSlotsParent);
            slotGO.transform.Find("Icon").GetComponent<Image>().sprite = invSlot.item.icon;
            slotGO.transform.Find("Amount").GetComponent<TextMeshProUGUI>().text = invSlot.quantity.ToString();

            // Aqui você pode adicionar botões para equipar/desequipar
            // slotGO.GetComponent<Button>().onClick.AddListener(() => EquiparItem(invSlot.item));

            equipamentosSlots.Add(slotGO);
        }
    }
}
