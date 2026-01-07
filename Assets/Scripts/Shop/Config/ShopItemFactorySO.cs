using UnityEngine;

public abstract class ShopItemFactorySO : ScriptableObject
{
    /// <summary>
    /// Cria uma instância do item para venda.
    /// </summary>
    /// <param name="context">O estado atual da Run (permite preços dinâmicos ou lógica condicional).</param>
    public abstract IPurchasable CreateItem(RunData context);
}