using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EquipmentInfoPanel : MonoBehaviour
{
    [Header("Elementos Básicos")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI weightText;

    [System.Serializable]
    public class StatUI
    {
        public Image statIcon;
        public ToolTipTrigger tooltipTrigger;
        public TextMeshProUGUI valueText;
    }

    [Header("Status")]
    public List<StatUI> statUIs = new(); // Coloque 3 elementos no Inspector

    private void OnEnable()
    {
        EquipmentSlotUI.OnEquipmentSelected += ShowEquipmentInfo;
    }

    private void OnDisable()
    {
        EquipmentSlotUI.OnEquipmentSelected -= ShowEquipmentInfo;
    }

    public void ShowEquipmentInfo(Equipment equipment)
    {
        if (equipment == null) return;

        gameObject.SetActive(true);

        icon.sprite = equipment.icon;
        nameText.text = equipment.equipmentName;
        descriptionText.text = equipment.description;
        rarityText.text = $"Raridade: {equipment.rarity}";
        weightText.text = $"Peso: {equipment.weight} kg";

        for (int i = 0; i < statUIs.Count; i++)
        {
            if (i < equipment.statModifiers.Count)
            {
                var stat = equipment.statModifiers[i];
                var ui = statUIs[i];

                ui.statIcon.gameObject.SetActive(true);
                ui.valueText.gameObject.SetActive(true);
                ui.valueText.text = $"+{stat.value}";
                ui.tooltipTrigger.content = stat.statName;
            }
            else
            {
                // Esconde os slots não usados
                statUIs[i].statIcon.gameObject.SetActive(false);
                statUIs[i].valueText.gameObject.SetActive(false);
            }
        }
    }
}
