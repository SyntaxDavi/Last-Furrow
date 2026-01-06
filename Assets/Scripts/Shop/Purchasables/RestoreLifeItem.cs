using UnityEngine;

public class RestoreLifeItem : IPurchasable
{
    public string DisplayName => "Chá Revigorante";
    public string Description => "Recupera 1 Coração.";
    // TODO: Idealmente injetar sprite via construtor ou Library
    public Sprite Icon => null;
    public int Price => 50;

    public PurchaseFailReason CanPurchase(PurchaseContext ctx)
    {
        if (ctx.RunData.CurrentLives >= ctx.RunData.MaxLives)
            return PurchaseFailReason.ConditionsNotMet;

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext ctx)
    {
        ctx.RunData.CurrentLives++;

        // CORREÇÃO DO FEEDBACK: Usa o evento injetado pelo contexto
        ctx.ProgressionEvents.TriggerLivesChanged(ctx.RunData.CurrentLives);
    }
}