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
        // 1. SANITIZAÇÃO (Fallback Seguro em vez de Crash)
        // Se vier errado, corrigimos para um valor seguro e avisamos o dev, mas o jogo continua.

        if (string.IsNullOrWhiteSpace(name))
        {
            DisplayName = "Item de Cura (Nome Inválido)";
            Debug.LogWarning("[RestoreLifeItem] Configuração inválida: Nome vazio.");
        }
        else
        {
            DisplayName = name;
        }

        if (healAmount <= 0)
        {
            _healAmount = 1; // Fallback para 1
            Debug.LogWarning($"[RestoreLifeItem] '{DisplayName}': Cura <= 0. Forçando para 1.");
        }
        else
        {
            _healAmount = healAmount; 
        }

        if (price < 0)
        {
            Price = 0; // Fallback para grátis
            Debug.LogWarning($"[RestoreLifeItem] '{DisplayName}': Preço negativo. Forçando para 0.");
        }
        else
        {
            Price = price;
        }

        // 2. DESCRIÇÃO DINÂMICA
        // Agora _healAmount já tem valor garantido.
        Description = !string.IsNullOrWhiteSpace(desc)
            ? desc
            : $"Recupera {_healAmount} coração(ões).";

        Icon = icon;
    }

    public PurchaseFailReason CanPurchase(PurchaseContext ctx)
    {
        // Uso do método encapsulado no RunData
        if (ctx.RunData.IsHealthFull())
            return PurchaseFailReason.ConditionsNotMet;

        return PurchaseFailReason.None;
    }

    public void OnPurchased(PurchaseContext ctx)
    {
        // 3. EXECUÇÃO VIA DOMÍNIO
        ctx.RunData.Heal(_healAmount);

        // Notificação
        ctx.ProgressionEvents.TriggerLivesChanged(ctx.RunData.CurrentLives);

        // Removemos o Debug.Log daqui (Feedback aceito: não poluir log de produção)
    }
}