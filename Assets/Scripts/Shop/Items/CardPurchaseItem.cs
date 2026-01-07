using UnityEngine;

public class CardPurchaseItem : IPurchasable
{
    private readonly CardData _cardAsset;
    private readonly int _price;

    public CardPurchaseItem(CardData card, int price = -1)
    {
        _cardAsset = card;
        // Se preço não for informado, calcula baseado no custo * multiplicador (Ex: 5)
        _price = price > 0 ? price : Mathf.Max(1, card.Cost * 5);
    }

    public string DisplayName => _cardAsset.Name;
    public string Description => "Adiciona à mão.";
    public Sprite Icon => _cardAsset.Icon;
    public int Price => _price;

    public PurchaseFailReason CanPurchase(PurchaseContext ctx)
    {
        // Regra de Negócio: Não pode comprar se mão cheia
        if (ctx.RunData.Hand.Count >= ctx.RunData.MaxHandSize)
            return PurchaseFailReason.ConditionsNotMet;

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext ctx)
    {
        // 1. Adiciona aos dados
        var newInstance = new CardInstance(_cardAsset.ID);
        ctx.RunData.Hand.Add(newInstance);

        // 2. Avisa sistemas visuais (sem acoplamento direto)
        ctx.PlayerEvents.TriggerCardAdded(newInstance);
    }
}