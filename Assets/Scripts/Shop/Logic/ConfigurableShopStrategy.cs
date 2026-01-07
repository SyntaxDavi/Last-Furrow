using System.Collections.Generic;

//Se não for “orquestrar fontes”, não entra aqui.
public class ConfigurableShopStrategy : IShopStrategy
{
    private readonly ShopProfileSO _profile;

    public ConfigurableShopStrategy(ShopProfileSO profile)
    {
        _profile = profile;
    }

    public string ShopTitle => _profile.ShopTitle;

    public List<IPurchasable> GenerateInventory(RunData run, IGameLibrary library)
    {
        var fullStock = new List<IPurchasable>();

        if (_profile.InventorySources == null) return fullStock;

        // Itera sobre cada fonte configurada no perfil
        foreach (var source in _profile.InventorySources)
        {
            if (source != null)
            {
                // Delega a geração para a fonte especializada, passando o contexto
                var items = source.GenerateItems(run, library);
                fullStock.AddRange(items);
            }
        }

        return fullStock;
    }
}