using System.Collections.Generic;

public interface IShopStrategy
{
    string ShopTitle { get; }
    List<IPurchasable> GenerateInventory(RunData run, IGameLibrary library);
}