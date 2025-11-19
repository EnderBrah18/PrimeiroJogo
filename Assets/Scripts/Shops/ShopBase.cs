using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopBase : MonoBehaviour
{
    public enum ShopType { Sell, Equipment, Upgrade }
    public ShopType shopType;

    [Header("Dados do Shop")]
    [SerializeField] protected ShopSO shopData;  // ScriptableObject do vendedor

    public ShopManager shopManager;

    [Serializable]
    public class ShopItemReference
    {
        public ItemSO item;          // Item da loja
        public Button button;        // Botão já existente na UI
        public TMP_Text label;       // Texto que mostra o nome/preço
        public TMP_Text feedback;    // Texto opcional para mensagens
        public Image icon;           // <<< ADICIONADO: imagem do item
        public bool isBuyButton = true;
        public int amount = 1;

        [Header("Limite de Compra")]
        public bool useBuyLimit = false;
        public int buyLimit = 0;
        [HideInInspector] public int currentBuys = 0;
    }

    [Header("Referências do Jogador")]
    [SerializeField] protected GameObject player;
    [SerializeField] protected PlayerStatsSO playerStatsSO;
    [SerializeField] private GameObject inventoryManager;

    [Header("Itens da Loja")]
    [SerializeField] private List<ShopItemReference> shopItems;

    [Header("Configuração")]
    public bool showPrices = true;
    public string coinSymbol = "🪙";

    // Referências internas
    protected Player playerStats;
    protected PlayerInventory playerInventoryRef;
    protected Inventory inventory;
    protected InventoryUI inventoryUI;

    protected virtual void InitializeShop()
    {
        if (player == null)
        {
            Player foundPlayer = FindFirstObjectByType<Player>();
            if (foundPlayer != null)
                player = foundPlayer.gameObject;
            else
                Debug.LogError("ShopBase não encontrou nenhum Player na cena!");
        }

        if (player != null)
        {
            playerStats = player.GetComponent<Player>();
            playerStatsSO = playerStats.Stats; // <-- acessar o SO via getter
        }

        // InventoryManager automático
        if (inventoryManager == null)
        {
            InventoryUI foundInv = FindFirstObjectByType<InventoryUI>();
            if (foundInv != null)
                inventoryManager = foundInv.gameObject;
            else
                Debug.LogError("ShopBase não encontrou InventoryManager (InventoryUI) na cena!");
        }

        playerInventoryRef = player.GetComponent<PlayerInventory>();
        if (playerInventoryRef == null)
        {
            Debug.LogError("PlayerInventory não encontrado no Player!");
            return;
        }


        inventory = playerStats.playerInventory.inventory;
        inventoryUI = inventoryManager.GetComponent<InventoryUI>();
        SaveSystem.Instance.RegisterSOSavable(shopData);

    }

    public void SaveShopToSO()
    {
        if (shopData == null)
        {
            Debug.LogWarning("ShopSO não atribuído!");
            return;
        }

        shopData.items.Clear();

        foreach (var refItem in shopItems)
        {
            if (refItem.item == null) continue;

            ShopSO.ShopItem soItem = new ShopSO.ShopItem
            {
                item = refItem.item,
                price = refItem.item.cost,
                stock = refItem.useBuyLimit ? refItem.buyLimit - refItem.currentBuys : -1
            };

            shopData.items.Add(soItem);
        }

        Debug.Log("ShopBase salvo no SO!");
    }

    private void Start()
    {
        InitializeShop();
        SaveShopToSO();

        // --- Configura cada botão da loja ---
        foreach (var refItem in shopItems)
        {
            if (refItem == null || refItem.button == null || refItem.item == null)
                continue;

            // Atualiza texto inicial
            if (refItem.label != null)
            {
                string priceText = showPrices ? $" - {refItem.item.cost}{coinSymbol}" : "";
                refItem.label.text = $"{refItem.item.itemName}{priceText}";
            }

            if (refItem.icon != null && refItem.item.icon != null)
            {
                refItem.icon.sprite = refItem.item.icon;
            }


            // Garante que não adiciona listeners duplicados
            refItem.button.onClick.RemoveAllListeners();

            // Captura da referência local (pra não bugar o closure no loop)
            var localRef = refItem;

            refItem.button.onClick.AddListener(() =>
            {
                if (localRef.isBuyButton)
                    BuyItem(localRef);
                else
                    SellItem(localRef);
            });
        }

        if (player != null)
        {
            player.transform.position = new Vector3(300, 15, 235);
            Debug.Log("Player movido para o centro do mapa.");
        }
    }

    // --- Compra ---
    private bool BuyItem(ShopItemReference refItem)
    {
        if (inventory == null || inventoryUI == null || playerStats == null)
        {
            Debug.LogWarning("ShopBase não configurado corretamente!");
            return false;
        }

        if (refItem == null || refItem.item == null)
        {
            Debug.LogWarning("Referência de item inválida!");
            return false;
        }

        // 🧩 Verifica o limite de compra individual
        if (refItem.useBuyLimit)
        {
            if (refItem.buyLimit <= 0)
            {
                Debug.Log($"Item {refItem.item.itemName} está com limite de compra 0.");
                return false;
            }

            if (refItem.currentBuys + refItem.amount > refItem.buyLimit)
            {
                ShowFeedback(refItem, "Limite de compra atingido!");
                Debug.Log($"Limite de compra atingido para {refItem.item.itemName} ({refItem.currentBuys}/{refItem.buyLimit}).");
                return false;
            }
        }

        int totalCost = refItem.item.cost * refItem.amount;

        if (!playerInventoryRef.SpendCoins(totalCost))
        {
            ShowFeedback(refItem, "Moedas insuficientes!");
            Debug.Log("Moedas insuficientes!");
            return false;
        }

        if (inventory.AddItem(refItem.item, refItem.amount))
        {
            if (refItem.useBuyLimit)
            {
                refItem.currentBuys += refItem.amount;

                // Atualiza estoque no SO
                var soEntry = shopData.GetItem(refItem.item);
                if (soEntry != null && soEntry.stock > 0)
                    soEntry.stock -= refItem.amount;
            }

            inventoryUI.UpdateCoinsUI();
            ShowFeedback(refItem, $"Comprado {refItem.amount}x {refItem.item.itemName}");
            return true;
        }

        // Se não conseguiu adicionar, reembolsa
        playerInventoryRef.coins += totalCost;
        inventoryUI.UpdateCoinsUI();
        ShowFeedback(refItem, "Inventário cheio!");
        Debug.Log("Inventário cheio ou erro na compra.");
        return false;
    }

    // --- Venda ---
    private void SellItem(ShopItemReference refItem)
    {
        if (inventory == null || refItem.item == null) return;

        if (!inventory.RemoveItem(refItem.item, refItem.amount))
        {
            ShowFeedback(refItem, "Você não tem esse item!");
            return;
        }

        int totalValue = refItem.item.cost * refItem.amount;
        playerInventoryRef.coins += totalValue;
        inventoryUI.UpdateCoinsUI();

        ShowFeedback(refItem, $"Vendeu {refItem.amount}x {refItem.item.itemName}");
        Debug.Log($"Vendeu {refItem.amount}x {refItem.item.itemName}");
    }

    // --- Mensagem opcional na UI ---
    private void ShowFeedback(ShopItemReference refItem, string message)
    {
        if (refItem.feedback != null)
        {
            refItem.feedback.text = message;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 1.5f);
        }
    }

    private void ClearFeedback()
    {
        foreach (var item in shopItems)
        {
            if (item.feedback != null)
                item.feedback.text = "";
        }
    }

    public void ResetAllBuyLimits()
    {
        foreach (var refItem in shopItems)
        {
            refItem.currentBuys = 0;
        }
    }

    public void OpenShop()
    {
        if (shopManager == null)
        {
            Debug.LogWarning("ShopManager não atribuído no vendedor!");
            return;
        }

        switch (shopType)
        {
            case ShopType.Sell:
                shopManager.ToggleSellShop();
                break;

            case ShopType.Equipment:
                shopManager.ToggleEquipmentShop();
                break;

            case ShopType.Upgrade:
                shopManager.ToggleUpgradeShop();
                break;
        }
    }   
}
