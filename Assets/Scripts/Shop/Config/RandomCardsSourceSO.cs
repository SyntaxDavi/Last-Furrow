using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Sources/Random Cards")]
public class RandomCardsSourceSO : ShopInventorySourceSO
{
    [Header("Regras de Geração")]
    [Min(1)][SerializeField] private int _amount = 3;

    [Tooltip("Se verdadeiro, usa o preço padrão da carta. Se falso, pode aplicar desconto/aumento.")]
    [SerializeField] private bool _useDefaultPrice = true;
    [SerializeField] private int _overridePrice = 10;

    public override IEnumerable<IPurchasable> GenerateItems(RunData run, IGameLibrary library)
    {
        var results = new List<IPurchasable>();

        // Aqui a fonte pede dados à library, isolando a lógica
        var randomCards = library.GetRandomCards(_amount);

        foreach (var cardData in randomCards)
        {
            int price = _useDefaultPrice ? -1 : _overridePrice;

            // Isolamos a criação do CardPurchaseItem aqui. 
            // A Strategy principal não sabe mais que essa classe existe.
            results.Add(new CardPurchaseItem(cardData, price));
        }

        return results;
    }
}