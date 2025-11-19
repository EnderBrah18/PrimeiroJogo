using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "RPG/Shop Data")]
public class ShopSO : ScriptableObject, ISOSavable
{
    [Header("Shop Info")]
    public string shopName;

    [Header("Items for Sale")]
    public List<ShopItem> items = new List<ShopItem>();

    [Header("Upgrades")]
    public List<UpgradeProgress> upgradesPurchased = new List<UpgradeProgress>();

    [Serializable]
    public class ShopItem
    {
        public ItemSO item;      // referência ao item
        public int price;        // preço do item
        public int stock = -1;   // quantidade disponível (-1 = infinito)
    }

    [Serializable]
    public class UpgradeProgress
    {
        public string upgradeName;
        public int currentBuys;
    }

    // Opcional: métodos auxiliares
    public ShopItem GetItem(ItemSO item)
    {
        return items.Find(i => i.item == item);
    }

    private class ShopSaveData
    {
        public List<int> itemStock;                  // Estoque dos itens
        public List<UpgradeProgress> upgradesPurchased;
    }

    public string GetSaveKey() => "ShopState";

    public string SaveData()
    {
        ShopSaveData data = new ShopSaveData
        {
            itemStock = new List<int>(),
            upgradesPurchased = new List<UpgradeProgress>()
        };

        // Salva estoque de cada item
        foreach (var item in items)
            data.itemStock.Add(item.stock);

        // Salva upgrades comprados
        foreach (var upg in upgradesPurchased)
            data.upgradesPurchased.Add(new UpgradeProgress
            {
                upgradeName = upg.upgradeName,
                currentBuys = upg.currentBuys
            });

        return JsonUtility.ToJson(data);
    }

    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        ShopSaveData data = JsonUtility.FromJson<ShopSaveData>(json);
        if (data == null) return;

        // Carrega estoque dos itens
        for (int i = 0; i < items.Count && i < data.itemStock.Count; i++)
            items[i].stock = data.itemStock[i];

        // Carrega upgrades comprados
        upgradesPurchased = new List<UpgradeProgress>();
        if (data.upgradesPurchased != null)
            upgradesPurchased.AddRange(data.upgradesPurchased);
    }
}
