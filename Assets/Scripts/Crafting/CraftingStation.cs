using System.Collections.Generic;
using UnityEngine;

public class CraftingStation : InteractableBase
{
    public CraftingStationSO stationData;

    [Header("Memória interna da estação")]
    public List<StoredResource> storedResources = new List<StoredResource>();
    public List<RecipeSO> availableRecipes;

    // -------------------------------------
    // MÉTODO: Depositar itens
    // -------------------------------------
    public (bool success, string message) DepositItem(PlayerInventory player, ItemSO item, int amount)
    {
        if (!player.inventory.RemoveItem(item, amount))
            return (false, "Você não tem essa quantidade.");

        var stored = storedResources.Find(i => i.item == item);

        if (stored == null)
        {
            stored = new StoredResource { item = item, amount = amount };
            storedResources.Add(stored);
        }
        else
        {
            stored.amount += amount;
        }

        return (true, "Item depositado!");
    }

    // -------------------------------------
    // VERIFICAR se tem recursos para craftar
    // -------------------------------------
    public bool HasResourcesFor(RecipeSO recipe)
    {
        foreach (var ing in recipe.ingredients)
        {
            var stored = storedResources.Find(i => i.item == ing.item);
            if (stored == null || stored.amount < ing.amount)
                return false;
        }
        return true;
    }

    // -------------------------------------
    // DESCONTAR os recursos da memória
    // -------------------------------------
    private void ConsumeResources(RecipeSO recipe)
    {
        foreach (var ing in recipe.ingredients)
        {
            var stored = storedResources.Find(i => i.item == ing.item);
            stored.amount -= ing.amount;
        }
    }

    // -------------------------------------
    // CRAFTAR o item
    // -------------------------------------
    public (bool success, string message) Craft(RecipeSO recipe, PlayerInventory player)
    {
        if (recipe == null)
            return (false, "Erro: Receita inválida!");

        if (player == null || player.inventory == null)
            return (false, "Erro: Player ou inventário não encontrado!");

        if (recipe.resultItem == null)
            return (false, "Erro: Item de resultado inválido!");

        if (!HasResourcesFor(recipe))
            return (false, "Faltam recursos!");

        // Tenta adicionar pelo método centralizado do Inventory.
        bool added = player.inventory.AddItem(recipe.resultItem, recipe.resultAmount);
        if (!added)
        {
            Debug.LogWarning("CraftingStation.Craft: Falha ao adicionar item no inventário do player (sem espaço/peso).");
            return (false, "Inventário cheio! Não foi possível adicionar o item.");
        }

        // Só após adicionar com sucesso, desconta os recursos da estação.
        ConsumeResources(recipe);

        return (true, $"Craft concluído: {recipe.resultItem.itemName} x{recipe.resultAmount}");
    }

    public override void Interact()
    {
        CraftingStationManager.Instance.OpenStationUI(this);
    }
}
