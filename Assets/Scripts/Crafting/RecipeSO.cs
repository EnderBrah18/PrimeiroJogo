using UnityEngine;

[System.Serializable]
public class RecipeIngredient
{
    public ItemSO item;
    public int amount;
}

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class RecipeSO : ScriptableObject
{
    public string recipeName;
    public RecipeIngredient[] ingredients;
    public ItemSO resultItem;
    public int resultAmount = 1;
}
