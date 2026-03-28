using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    public static event Action OnShopManagerReady;

    [Header("UI Principal da Loja")]
    public GameObject ShopUI;
    private ThirdPersonCamera cameraScript;

    [Header("Sub-lojas dentro do Canvas")]
    public GameObject SellShopUI;
    public GameObject EquipmentShopUI;
    public GameObject UpgradeShopUI;

    private GameObject currentOpenShop = null;

    private bool blocked = false;

    void Awake()
    {
        Instance = this;
        OnShopManagerReady?.Invoke();

        // Tenta achar a câmera no início
        cameraScript = UnityEngine.Object.FindAnyObjectByType<ThirdPersonCamera>();
    }

    void Start()
    {
        CloseAll();
    }

    public void CloseAll()
    {
        currentOpenShop = null;

        ShopUI.SetActive(false);

        SellShopUI.SetActive(false);
        EquipmentShopUI.SetActive(false);
        UpgradeShopUI.SetActive(false);
        ShopUIManager.IsShopOpen = false;

        Player.Instance?.SetMovementBlocked(false);
        cameraScript?.HandleInventoryToggled(false);

        currentOpenShop = null;
    }

    private void OpenShop(GameObject shop)
    {
        // Fecha lojas anteriores
        CloseAll();

        // Abre apenas a loja solicitada
        ShopUI.SetActive(true);
        shop.SetActive(true);
        ShopUIManager.IsShopOpen = true;
        blocked = !blocked;

        Player.Instance?.SetMovementBlocked(blocked);
        Player.Instance?.SetAttackBlocked(blocked);
        cameraScript?.HandleInventoryToggled(true);

        currentOpenShop = shop;

    }

    // =====================
    //  Suporte à Câmera
    // =====================
    private void ToggleCameraMode(bool open)
    {
        // Se a câmera ainda não foi encontrada, tenta novamente
        if (cameraScript == null)
            cameraScript = UnityEngine.Object.FindAnyObjectByType<ThirdPersonCamera>();

        if (cameraScript != null)
        {
            cameraScript.HandleInventoryToggled(open);
        }
        else
        {
            Debug.LogWarning("ShopManager: Nenhum ThirdPersonCamera encontrado!");
        }
    }

    // === Chamados por cada vendedor ===

    public void ToggleSellShop()
    {
        if (currentOpenShop == SellShopUI)
            CloseAll();
        else
            OpenShop(SellShopUI);
    }

    public void ToggleUpgradeShop()
    {
        if (currentOpenShop == UpgradeShopUI)
            CloseAll();
        else
            OpenShop(UpgradeShopUI);
    }

    public void ToggleEquipmentShop()
    {
        if (currentOpenShop == EquipmentShopUI)
            CloseAll();
        else
            OpenShop(EquipmentShopUI);
    }

    //temporário
    public static class ShopUIManager
    {
        public static bool IsShopOpen = false;
    }
}