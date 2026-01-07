using System.Collections.Generic;

/// <summary>
/// Representa uma instância ativa de uma loja.
/// Resolve o problema de estado solto dentro do Service.
/// </summary>
public class ShopSession
{
    public string Title { get; private set; }
    public List<IPurchasable> Stock { get; private set; }

    public ShopSession(string title, List<IPurchasable> initialStock)
    {
        Title = title;
        Stock = initialStock ?? new List<IPurchasable>();
    }

    /// <summary>
    /// Tenta remover um item do estoque.
    /// Futuro: Aqui entra validação por ID/GUID em vez de referência.
    /// </summary>
    public bool TryRemoveItem(IPurchasable item)
    {
        if (Stock.Contains(item))
        {
            Stock.Remove(item);
            return true;
        }
        return false;
    }
}