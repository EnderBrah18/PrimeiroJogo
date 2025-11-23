using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingStationUI : MonoBehaviour
{
    [Header("Referências principais")]
    public CraftingStation station;
    private List<InventorySlot> dropdownSlots = new List<InventorySlot>();

    [SerializeField] protected GameObject playerRef;
    public PlayerInventory player;

    [Header("UI de Depósito")]
    public TMP_Dropdown dropdownItems;
    public TMP_InputField inputDepositAmount;
    public Button buttonDeposit;

    [Header("UI de Armazenamento")]
    public Transform contentStorage;
    public GameObject storageItemPrefab;

    [Header("UI de Receitas")]
    public Transform contentRecipes;
    public GameObject recipeButtonPrefab;

    [Header("Mensagem")]
    public TMP_Text infoText;

    // marca se já estamos inscritos nos eventos do inventário
    private bool inventorySubscribed = false;

    private void Start()
    {
        InitializeCraftingStation();

        if (player != null)
        {
            RefreshUI();
        }
        else
        {
            Debug.LogError("CraftingStationUI: RefreshUI chamado sem player!");
        }

        if (buttonDeposit != null)
            buttonDeposit.onClick.AddListener(DepositSelectedItem);
        else
            Debug.LogWarning("CraftingStationUI: buttonDeposit não está setado na UI!");
    }

    protected virtual void InitializeCraftingStation()
    {
        if (playerRef == null)
        {
            Player foundPlayer = FindFirstObjectByType<Player>();
            if (foundPlayer == null)
            {
                Debug.LogError("CraftingStationUI: Não encontrou objeto Player na cena!");
                return;
            }
            playerRef = foundPlayer.gameObject;
        }

        player = playerRef.GetComponent<PlayerInventory>();

        if (player == null)
        {
            Debug.LogError("CraftingStationUI: PlayerInventory não encontrado no Player!");
            return;
        }

        if (player.inventory == null)
        {
            Debug.LogError("CraftingStationUI: player.inventory é NULL! O inventário do player ainda não foi inicializado.");
            return;
        }

        // Inscreve-se no evento de mudança do inventário para atualizar o dropdown automaticamente
        SubscribeToInventory();
    }

    private void OnEnable()
    {
        // garante inscrição caso o player tenha sido definido antes do OnEnable
        if (!inventorySubscribed && player != null && player.inventory != null)
            SubscribeToInventory();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventory();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInventory();
    }

    private void SubscribeToInventory()
    {
        if (player == null || player.inventory == null || inventorySubscribed)
            return;

        // A classe Inventory dispara OnInventoryChanged(ItemType) quando algo muda.
        player.inventory.OnInventoryChanged -= HandleInventoryChanged;
        player.inventory.OnInventoryChanged += HandleInventoryChanged;
        inventorySubscribed = true;
    }

    private void UnsubscribeFromInventory()
    {
        if (player == null || player.inventory == null || !inventorySubscribed)
            return;

        player.inventory.OnInventoryChanged -= HandleInventoryChanged;
        inventorySubscribed = false;
    }

    private void HandleInventoryChanged(ItemType type)
    {
        // atualiza a UI quando o inventário do player mudar
        RefreshUI();
    }

    // ---------------------------------------------------------
    // Atualização geral da UI
    // ---------------------------------------------------------
    public void RefreshUI()
    {
        if (player == null || player.inventory == null)
        {
            Debug.LogError("CraftingStationUI: Não é possível atualizar a UI, player ou inventário não inicializado!");
            return;
        }

        LoadPlayerItemsInDropdown();
        RefreshStorageList();
        RefreshRecipeList();
        // NÃO zere a mensagem aqui — ShowInfo gerencia o tempo de exibição.
        // infoText ficará sendo limpa apenas por ClearInfo() chamado dentro de ShowInfo.
    }

    // ---------------------------------------------------------
    // Dropdown de itens do inventário
    // ---------------------------------------------------------
    private void LoadPlayerItemsInDropdown()
    {
        if (dropdownItems == null)
        {
            Debug.LogError("CraftingStationUI: dropdownItems não está setado na UI!");
            return;
        }

        dropdownItems.ClearOptions();
        dropdownSlots.Clear();

        List<string> names = new List<string>();

        // Agrega todos os tipos de slots do inventário do player (chest usa slots list; player usa listas separadas)
        List<InventorySlot> allSlots = new List<InventorySlot>();
        if (player.inventory.slots != null)
            allSlots.AddRange(player.inventory.slots);

        if (player.inventory.resourceSlots != null)
            allSlots.AddRange(player.inventory.resourceSlots);
        if (player.inventory.equipmentSlots != null)
            allSlots.AddRange(player.inventory.equipmentSlots);
        if (player.inventory.consumableSlots != null)
            allSlots.AddRange(player.inventory.consumableSlots);
        if (player.inventory.questItemSlots != null)
            allSlots.AddRange(player.inventory.questItemSlots);

        // Se não houver slots, informar
        if (allSlots.Count == 0)
        {
            Debug.LogWarning("CraftingStationUI: inventário não possui slots configurados!");
        }

        // Percorre todos os slots agregados
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.item != null)
            {
                names.Add($"{slot.item.itemName} x{slot.quantity}");
                dropdownSlots.Add(slot);
            }
        }

        dropdownItems.AddOptions(names);
    }

    // ---------------------------------------------------------
    // Depositar o item selecionado
    // ---------------------------------------------------------
    void DepositSelectedItem()
    {
        if (station == null || player == null)
        {
            ShowInfo("Estação ou player não configurado.");
            return;
        }

        if (dropdownSlots.Count == 0)
        {
            ShowInfo("Inventário vazio.");
            return;
        }

        int index = dropdownItems.value;
        if (index < 0 || index >= dropdownSlots.Count)
        {
            ShowInfo("Seleção inválida.");
            return;
        }

        var slot = dropdownSlots[index];
        if (slot == null || slot.item == null)
        {
            ShowInfo("Slot vazio.");
            return;
        }

        if (inputDepositAmount == null || !int.TryParse(inputDepositAmount.text, out int amount) || amount <= 0)
        {
            ShowInfo("Quantidade inválida.");
            return;
        }

        var depositResult = station.DepositItem(player, slot.item, amount);
        ShowInfo(depositResult.message);
        if (depositResult.success)
            RefreshUI();
    }

    // ---------------------------------------------------------
    // Lista de recursos armazenados
    // ---------------------------------------------------------
    void RefreshStorageList()
    {
        foreach (Transform t in contentStorage)
        {
            Destroy(t.gameObject);
        }

        foreach (var entry in station.storedResources)
        {
            var obj = Instantiate(storageItemPrefab, contentStorage);
            obj.GetComponentInChildren<TMP_Text>().text = $"{entry.item.itemName}: {entry.amount}";
        }
    }

    // ---------------------------------------------------------
    // Lista de receitas
    // ---------------------------------------------------------
    void RefreshRecipeList()
    {
        foreach (Transform t in contentRecipes)
            Destroy(t.gameObject);

        foreach (var recipe in station.availableRecipes)
        {
            var obj = Instantiate(recipeButtonPrefab, contentRecipes);
            var ui = obj.GetComponent<RecipeButtonUI>();

            ui.recipeNameText.text = recipe.recipeName;

            string reqs = "";
            foreach (var req in recipe.ingredients)
                reqs += $"{req.item.itemName} x{req.amount}\n";
            ui.requirementsText.text = reqs.TrimEnd('\n');

            ui.craftButton.onClick.AddListener(() =>
            {
                if (player == null)
                {
                    ShowInfo("Player não está inicializado!");
                    return;
                }

                var craftResult = station.Craft(recipe, player);
                ShowInfo(craftResult.message);
                if (craftResult.success)
                    RefreshUI();
            });
        }
    }

    void ShowInfo(string txt, float duration = 3f)
    {
        if (infoText == null)
        {
            Debug.LogWarning("CraftingStationUI: infoText não atribuído no Inspector. Mensagem: " + txt);
            return;
        }

        infoText.text = txt;
        CancelInvoke(nameof(ClearInfo));
        Invoke(nameof(ClearInfo), duration);
    }

    void ClearInfo()
    {
        if (infoText != null)
            infoText.text = "";
    }
}
