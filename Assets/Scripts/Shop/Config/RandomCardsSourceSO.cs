using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Sources/Random Cards")]
public class RandomCardsSourceSO : ShopInventorySourceSO
{
    [Header("Regras de Geração")]
    [Min(1)][SerializeField] private int _amount = 3;

    [Header("Preço")]
    [SerializeField] private bool _useDefaultPrice = true;
    [SerializeField][Min(0)] private int _fixedPrice = 10; // Nome mais claro que overridePrice

    // MUDANÇA: Retorno explícito de List (Materializado)
    public override List<IPurchasable> GenerateItems(RunData run, IGameLibrary library)
    {
        var results = new List<IPurchasable>();
        var randomCards = library.GetRandomCards(_amount);

        foreach (var cardData in randomCards)
        {
            // Lógica de preço mais clara
            int? price = _useDefaultPrice ? (int?)null : _fixedPrice;

            // Usa o Builder centralizado em vez de dar new direto
            // Se amanhã CardPurchaseItem mudar, só mexemos no Builder.
            results.Add(ShopItemBuilder.CreateCardItem(cardData, price));
        }

        return results;
    }
}