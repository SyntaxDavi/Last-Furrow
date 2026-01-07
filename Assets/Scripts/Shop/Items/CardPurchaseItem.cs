using UnityEngine;
using System;

public class CardPurchaseItem : IPurchasable
{
    private readonly CardData _cardAsset;
    private readonly int _price;
    private readonly string _customDescription;

    // Constante para facilitar balanceamento futuro se não houver preço
    private const int DEFAULT_PRICE_MULTIPLIER = 5;

    public CardPurchaseItem(CardData card, int price = -1, string descriptionOverride = null)
    {
        // 1. VALIDAÇÃO (Fail Fast)
        // Impede que um item quebrado entre na loja e cause erros silenciosos depois
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card), "[CardPurchaseItem] Tentativa de criar item com CardData nulo.");
        }

        _cardAsset = card;
        _customDescription = descriptionOverride;

        // 2. CÁLCULO DE PREÇO SEGURO
        // Se preço for inválido (-1 ou 0), aplicamos a regra de negócio padrão.
        if (price <= 0)
        {
            // Mathf.Max garante que nunca custe 0 ou negativo, mesmo que o custo da carta seja 0
            _price = Mathf.Max(1, card.Cost * DEFAULT_PRICE_MULTIPLIER);
        }
        else
        {
            _price = price;
        }
    }

    public string DisplayName => _cardAsset.Name;

    // 3. DESCRIÇÃO DINÂMICA
    // Prioridade: Override do Construtor > Descrição da Carta > Texto Genérico
    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(_customDescription)) return _customDescription;

            // Se o CardData tiver descrição no futuro, retornamos ela aqui
            // return _cardAsset.Description; 

            return "Adiciona uma cópia à sua mão."; // Texto genérico melhorado
        }
    }

    public Sprite Icon => _cardAsset.Icon;
    public int Price => _price;

    public PurchaseFailReason CanPurchase(PurchaseContext ctx)
    {
        // Regra de Negócio: Limite de Mão
        // Essa verificação é pura e não depende de UI
        if (ctx.RunData.Hand.Count >= ctx.RunData.MaxHandSize)
            return PurchaseFailReason.ConditionsNotMet;

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext ctx)
    {
        // 1. Mutação de Estado (Dados)
        var newInstance = new CardInstance(_cardAsset.ID);
        ctx.RunData.Hand.Add(newInstance);

        // 2. Notificação (Sistema)
        // Mantemos isso aqui por pragmatismo, mas sabendo que idealmente
        // a Hand dispararia esse evento ao ser modificada.
        ctx.PlayerEvents.TriggerCardAdded(newInstance);

        Debug.Log($"[Compra] Carta '{_cardAsset.Name}' adicionada à mão.");
    }
}