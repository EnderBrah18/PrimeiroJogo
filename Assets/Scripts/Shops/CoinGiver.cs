using UnityEngine;

public class CoinGiver : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject inventoryManager;

    public Player playerStats;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player não atribuído no CoinGiver!");
            return;
        }

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager não atribuído no CoinGiver!");
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


    public void GiveCoins(int amount)
    {
        if (inventory == null || inventoryUI == null)
        {
            Debug.LogWarning("Tentando dar moedas mas inventário/UI não estão configurados.");
            return;
        }

        inventory.coins += amount;
        inventoryUI.UpdateCoinsUI();
        Debug.Log($"Gave {amount} coins to the player. Total coins: {inventory.coins}");
    }

}
