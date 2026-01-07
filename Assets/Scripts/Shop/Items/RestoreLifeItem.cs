using UnityEngine;

public class RestoreLifeItem : IPurchasable
{
    // Propriedades vindas da Fábrica
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public int Price { get; }

    private readonly int _healAmount;

    // Construtor que recebe a configuração
    public RestoreLifeItem(string name, string desc, Sprite icon, int healAmount, int price)
    {
        DisplayName = name;
        Description = desc;
        Icon = icon;
        _healAmount = healAmount;
        Price = price;
    }

    public PurchaseFailReason CanPurchase(PurchaseContext ctx)
    {
        if (ctx.RunData.CurrentLives >= ctx.RunData.MaxLives)
            return PurchaseFailReason.ConditionsNotMet;

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext ctx)
    {
        ctx.RunData.CurrentLives += _healAmount;

        // Garante que não ultrapasse o máximo
        if (ctx.RunData.CurrentLives > ctx.RunData.MaxLives)
            ctx.RunData.CurrentLives = ctx.RunData.MaxLives;

        // Usa o evento injetado pelo contexto
        ctx.ProgressionEvents.TriggerLivesChanged(ctx.RunData.CurrentLives);
    }
}