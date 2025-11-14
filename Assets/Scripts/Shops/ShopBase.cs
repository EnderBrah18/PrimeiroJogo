using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBase : MonoBehaviour
{
    [Serializable]
    public class ShopItemReference
    {
        public ItemSO item;          // Item da loja
        public Button button;        // Botão já existente na UI
        public TMP_Text label;       // Texto que mostra o nome/preço
        public TMP_Text feedback;    // (Opcional) texto para mensagens tipo "Sem moedas"
        public bool isBuyButton = true;
        public int amount = 1;

        [Header("Limite de Compra")]
        public bool useBuyLimit = false;     // Se o item/slot tem limite
        public int buyLimit = 0;             // Máximo de compras permitidas
        [HideInInspector] public int currentBuys = 0;         // Quantas vezes já foi comprado
    }

    [Header("Referências do Jogador")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject inventoryManager;

    [Header("Itens da Loja")]
    [SerializeField] private List<ShopItemReference> shopItems;

    [Header("Configuração")]
    public bool showPrices = true;
    public string coinSymbol = "🪙";

    // Referências internas
    protected Player playerStats;
    protected Inventory inventory;
    protected InventoryUI inventoryUI;

    protected virtual void InitializeShop()
    {
        if (player == null)
        {
            Debug.LogError("Player não atribuído no ShopBase!");
            return;
        }

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager não atribuído no ShopBase!");
            return;
        }

        playerStats = player.GetComponent<Player>();
        inventory = player.GetComponent<PlayerInventory>().inventory;
        inventoryUI = inventoryManager.GetComponent<InventoryUI>();

        if (inventory == null)
            Debug.LogError("Player não tem componente Inventory!");

        if (inventoryUI == null)
            Debug.LogError("InventoryManager não tem componente InventoryUI!");
    }

    private void Start()
    {
        InitializeShop();

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

        if (!inventory.SpendCoins(totalCost))
        {
            ShowFeedback(refItem, "Moedas insuficientes!");
            Debug.Log("Moedas insuficientes!");
            return false;
        }

        if (inventory.AddItem(refItem.item, refItem.amount))
        {
            if (refItem.useBuyLimit)
                refItem.currentBuys += refItem.amount;

            inventoryUI.UpdateCoinsUI();
            ShowFeedback(refItem, $"Comprado {refItem.amount}x {refItem.item.itemName}");
            Debug.Log($"Comprado {refItem.amount}x {refItem.item.itemName} ({refItem.currentBuys}/{refItem.buyLimit})");
            return true;
        }

        // Se não conseguiu adicionar, reembolsa
        inventory.coins += totalCost;
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
        inventory.coins += totalValue;
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
}
