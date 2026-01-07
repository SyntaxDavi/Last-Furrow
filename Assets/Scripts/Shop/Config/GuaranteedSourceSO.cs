using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Sources/Guaranteed List")]
public class GuaranteedSourceSO : ShopInventorySourceSO
{
    [Header("Configuração")]
    [SerializeField] private List<ShopItemFactorySO> _itemsToCreate;

    public override IEnumerable<IPurchasable> GenerateItems(RunData run, IGameLibrary library)
    {
        var results = new List<IPurchasable>();

        if (_itemsToCreate == null) return results;

        foreach (var factory in _itemsToCreate)
        {
            if (factory != null)
            {
                // Passamos o RunData (Contexto) corretamente para a fábrica
                var item = factory.CreateItem(run);
                if (item != null)
                {
                    results.Add(item);
                }
            }
        }

        return results;
    }
}