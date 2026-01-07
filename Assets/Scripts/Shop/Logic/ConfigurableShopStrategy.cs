using System.Collections.Generic;


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
        var stock = new List<IPurchasable>();

        // 1. Gera Itens Garantidos (Passando o Contexto!)
        if (_profile.GuaranteedItems != null)
        {
            foreach (var factory in _profile.GuaranteedItems)
            {
                if (factory != null)
                {
                    // MUDANÇA CRÍTICA: Passamos 'run' para a fábrica.
                    // Agora a fábrica tem poder de decisão baseado no estado do jogo.
                    IPurchasable item = factory.CreateItem(run);

                    if (item != null)
                    {
                        stock.Add(item);
                    }
                }
            }
        }

        // 2. Gera Cartas Aleatórias
        if (_profile.IncludeRandomCards)
        {
            var randomCards = library.GetRandomCards(_profile.RandomCardsCount);
            foreach (var card in randomCards)
            {
                // Cartas aleatórias usam preço padrão (-1)
                // Nota: Poderíamos criar uma fábrica genérica para Random Cards também no futuro
                stock.Add(new CardPurchaseItem(card, -1));
            }
        }

        return stock;
    }
}