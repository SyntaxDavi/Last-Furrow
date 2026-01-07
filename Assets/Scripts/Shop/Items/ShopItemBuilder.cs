public static class ShopItemBuilder
{
    public static IPurchasable CreateCardItem(CardData card, int? overridePrice = null)
    {
        // Centraliza a regra de -1 aqui, se o item interno ainda precisar
        int price = overridePrice.HasValue ? overridePrice.Value : -1;
        return new CardPurchaseItem(card, price);
    }
}