using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ItemLoader
{
    // Dicionário para acesso rápido por ID
    private static Dictionary<string, ItemSO> _itemCache;

    // Flag para garantir que carregou só uma vez
    private static bool _isLoaded = false;

    /// <summary>
    /// Carrega todos os itens do projeto em tempo de execução (editor ou build)
    /// </summary>
    private static void LoadAllItems()
    {
        if (_isLoaded) return;

        _itemCache = new Dictionary<string, ItemSO>();

        // Carrega todos os ScriptableObjects que herdam de ItemSO
        ItemSO[] allItems = Resources.LoadAll<ItemSO>(""); // "" = raiz da pasta Resources

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.id))
            {
                Debug.LogWarning($"ItemSO sem ID definido detectado: {item.name}. Preencha o campo 'id'.");
                continue; // ignora esse item
            }

            if (!_itemCache.ContainsKey(item.id))
                _itemCache.Add(item.id, item);
            else
                Debug.LogWarning($"ItemSO duplicado detectado com ID: {item.id}");
        }

        _isLoaded = true;
        Debug.Log($"ItemLoader: {_itemCache.Count} itens carregados.");
    }

    /// <summary>
    /// Retorna o item pelo ID
    /// </summary>
    public static ItemSO GetItemByID(string id)
    {
        if (!_isLoaded)
            LoadAllItems(); // só carrega uma vez

        if (string.IsNullOrEmpty(id)) return null;

        if (_itemCache.TryGetValue(id, out var item))
            return item;

        Debug.LogWarning($"Item não encontrado: {id}");
        return null;
    }

    /// <summary>
    /// Retorna todos os itens carregados
    /// </summary>
    public static IEnumerable<ItemSO> GetAllItems()
    {
        LoadAllItems();
        return _itemCache.Values;
    }
}
