using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Sources/Guaranteed List")]        
public class GuaranteedSourceSO : ShopInventorySourceSO
{
    [Header("Configuração")]
    [SerializeField] private List<ShopItemFactorySO> _itemsToCreate;

    public override List<IPurchasable> GenerateItems(
        RunData run,
        IGameLibrary library,
        IRandomProvider random)
    {
        var results = new List<IPurchasable>();

        if (_itemsToCreate == null)
            return results;

        foreach (var factory in _itemsToCreate)
        {
            if (factory == null)
                continue;

            var item = factory.CreateItem(run);
            if (item != null)
                results.Add(item);
        }

        return results;
    }

}
