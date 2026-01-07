using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Factories/Fixed Card Item")] 
public class CardItemFactorySO : ShopItemFactorySO
{
    [SerializeField] private CardData _card;

    [Header("Economia")]
    [SerializeField] private bool _useCustomPrice = false;
    [SerializeField][Min(0)] private int _customPrice = 10;

    public override IPurchasable CreateItem(RunData context)
    {
        int? price = _useCustomPrice ? _customPrice : (int?)null;

        // Usa o mesmo Builder que a fonte aleatória usa
        return ShopItemBuilder.CreateCardItem(_card, price);
    }
}