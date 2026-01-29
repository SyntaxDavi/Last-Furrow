using System.Collections.Generic;

public interface IShopStrategy
{
    string ShopTitle { get; }
    // INJETANDO RandomProvider para que as estratégias possam gerar itens deterministicamente
    List<IPurchasable> GenerateInventory(RunData run, IGameLibrary library, IRandomProvider random);
}