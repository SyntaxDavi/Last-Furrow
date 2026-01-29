using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Sources/Random Cards")]
public class RandomCardsSourceSO : ShopInventorySourceSO
{
    [Header("Regras de Geração")]
    [Min(1)][SerializeField] private int _amount = 3;

    [Header("Preço")]
    [SerializeField] private bool _useDefaultPrice = true;
    [SerializeField][Min(0)] private int _fixedPrice = 10;

    public override List<IPurchasable> GenerateItems(
        RunData run, 
        IGameLibrary library,
        IRandomProvider random)
    {
        var results = new List<IPurchasable>();
        
        // Agora passando o random provider para garantir determinismo
        var randomCards = library.GetRandomCards(_amount, random);

        foreach (var cardData in randomCards)
        {
            int? price = _useDefaultPrice ? (int?)null : _fixedPrice;
            results.Add(ShopItemBuilder.CreateCardItem(cardData, price));
        }

        return results;
    }
}
