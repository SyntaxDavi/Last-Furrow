using UnityEngine;

public class RestoreLifeItem : IPurchasable
{
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public int Price { get; }

    private readonly int _healAmount;

    public RestoreLifeItem(string name, string desc, Sprite icon, int healAmount, int price)
    {
        DisplayName = name;
        Price = price;
        
        // Se vier errado, corrigimos para um valor seguro
        if (healAmount <= 0)
            _healAmount = 1; // Fallback para 1
        else
            _healAmount = healAmount;

        // Agora _healAmount ja tem valor garantido.
        Description = string.IsNullOrEmpty(desc) 
            ? $"Recupera {_healAmount} coracao(oes)." 
            : desc;

        Icon = icon;
    }

    public PurchaseFailReason CanPurchase(PurchaseContext context)
    {
        if (context.Health.IsAtFullHealth)
        {
            return PurchaseFailReason.PlayerLifeFull;
        }

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext context)
    {
        context.Health.Heal(_healAmount);
    }
}
