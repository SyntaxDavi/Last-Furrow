using System.Collections.Generic;

public class WeekendShopStrategy : IShopStrategy
{
    public string ShopTitle => "Loja";

    public List<IPurchasable> GenerateInventory(RunData run, IGameLibrary library)
    {
        var stock = new List<IPurchasable>();

        // 1. Sempre oferece cura se estiver machucado (Mitigação)
        if (run.CurrentLives < run.MaxLives)
        {
            stock.Add(new RestoreLifeItem());
        }

        // 2. Oferece 3 Cartas Aleatórias (Expansão)
        // No futuro: GetCardsByRarity(run.CurrentWeek)
        var randomCards = library.GetRandomCards(3);

        foreach (var card in randomCards)
        {
            stock.Add(new CardPurchaseItem(card));
        }

        return stock;
    }
}