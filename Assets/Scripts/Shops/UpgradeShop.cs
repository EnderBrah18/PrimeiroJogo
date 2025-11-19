using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeShop : ShopBase
{
    [Serializable]
    public class UpgradeReference
    {
        [Header("Configuração do Upgrade")]
        public string upgradeName;
        public Button button;
        public TMP_Text label;
        public TMP_Text feedback;
        public int cost;
        public UpgradeType upgradeType;
        public float value;

        [Header("Limite de Compra")]
        public bool useBuyLimit = false;   // Se o upgrade pode ser comprado apenas X vezes
        public int buyLimit = 1;           // Máximo de compras
        [HideInInspector] public int currentBuys = 0; // Compras feitas
    }

    public enum UpgradeType
    {
        MoveSpeed,
        JumpForce,
        DashDistance,
        DashCooldown,
        MaxStamina,
        StaminaRegenRate,
        MaxWeight
        // Adicione mais upgrades aqui
    }

    [Header("Upgrades disponíveis")]
    [SerializeField] private List<UpgradeReference> upgrades;

    private void Start()
    {
        InitializeShop();

        foreach (var upg in upgrades)
        {
            if (upg.button == null) continue;

            var localRef = upg;
            upg.button.onClick.RemoveAllListeners();
            upg.button.onClick.AddListener(() => TryBuyUpgrade(localRef));

            // Atualiza o texto inicial do botão
            if (upg.label != null)
            {
                string price = showPrices ? $" - {upg.cost}{coinSymbol}" : "";
                upg.label.text = $"{upg.upgradeName}{price}";
            }
        }
    }

    private void TryBuyUpgrade(UpgradeReference upg)
    {
        if (inventory == null || playerStats == null)
        {
            Debug.LogWarning("UpgradeShop não está configurado corretamente!");
            return;
        }

        // 🧩 Verifica o limite de compra individual
        if (upg.useBuyLimit)
        {
            if (upg.buyLimit <= 0)
            {
                ShowUpgradeFeedback(upg, $"Upgrade {upg.upgradeName} não pode mais ser comprado.");
                Debug.Log($"[UpgradeShop] {upg.upgradeName} está com limite de compra 0.");
                return;
            }

            if (upg.currentBuys >= upg.buyLimit)
            {
                ShowUpgradeFeedback(upg, "Limite de compra atingido!");
                Debug.Log($"[UpgradeShop] Limite atingido para {upg.upgradeName} ({upg.currentBuys}/{upg.buyLimit}).");
                return;
            }
        }

        if (upg.useBuyLimit)
        {
            upg.currentBuys++;

            var progress = shopData.upgradesPurchased.Find(x => x.upgradeName == upg.upgradeName);
            if (progress == null)
            {
                progress = new ShopSO.UpgradeProgress
                {
                    upgradeName = upg.upgradeName,
                    currentBuys = upg.currentBuys
                };
                shopData.upgradesPurchased.Add(progress);
            }
            else
            {
                progress.currentBuys = upg.currentBuys;
            }
        }

        // 💰 Verifica se há moedas suficientes
        if (!playerInventoryRef.SpendCoins(upg.cost))
        {
            ShowUpgradeFeedback(upg, "Sem moedas suficientes!");
            Debug.Log($"[UpgradeShop] Moedas insuficientes para {upg.upgradeName}.");
            return;
        }

        // ⚙️ Aplica o upgrade
        ApplyUpgrade(upg);

        // 🔁 Atualiza contadores e UI
        if (upg.useBuyLimit)
            upg.currentBuys++;

        inventoryUI.UpdateCoinsUI();
        ShowUpgradeFeedback(upg, $"Upgrade '{upg.upgradeName}' comprado!");
        Debug.Log($"[UpgradeShop] Comprado {upg.upgradeName} ({upg.currentBuys}/{upg.buyLimit})");
    }

    private void ApplyUpgrade(UpgradeReference upg)
    {
        switch (upg.upgradeType)
        {
            case UpgradeType.MoveSpeed:
                playerStatsSO.movement.baseMoveSpeed += upg.value;
                break;
            case UpgradeType.JumpForce:
                playerStatsSO.movement.baseJumpForce += upg.value;
                break;
            case UpgradeType.DashDistance:
                playerStatsSO.dash.dashDistance += upg.value;
                break;
            case UpgradeType.DashCooldown:
                playerStatsSO.dash.dashCooldown = Mathf.Max(0.1f, playerStatsSO.dash.dashCooldown - upg.value);
                break;
            case UpgradeType.MaxStamina:
                playerStatsSO.stamina.maxStamina += upg.value;
                break;
            case UpgradeType.StaminaRegenRate:
                playerStatsSO.stamina.staminaRegenRate += upg.value;
                break;
            case UpgradeType.MaxWeight:
                playerStatsSO.inventory.maxWeight += upg.value;
                inventory.maxWeight = playerStatsSO.inventory.maxWeight;
                inventoryUI.UpdatePesoUI();
                break;
        }

        Debug.Log($"Aplicou upgrade {upg.upgradeName} ({upg.upgradeType}) +{upg.value}");
    }

    private void ShowUpgradeFeedback(UpgradeReference upg, string msg)
    {
        if (upg.feedback != null)
        {
            upg.feedback.text = msg;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 1.5f);
        }
    }

    private void ClearFeedback()
    {
        foreach (var u in upgrades)
            if (u.feedback != null)
                u.feedback.text = "";
    }

    
}
