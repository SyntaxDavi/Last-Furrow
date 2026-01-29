using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopService
{
    private readonly IEconomyService _economy;
    private readonly ISaveManager _saveManager;
    private readonly IGameLibrary _library;
    private readonly GameEvents _gameEvents;

    public ShopSession CurrentSession { get; private set; }

    // Helpers de acesso rápido para manter compatibilidade com a UI atual
    public List<IPurchasable> CurrentStock => CurrentSession?.Stock;
    public string CurrentShopTitle => CurrentSession?.Title;
           
    // Eventos
    public event Action OnStockRefreshed;
    public event Action<IPurchasable> OnItemPurchased;
    public event Action<PurchaseFailReason> OnPurchaseFailed;

    public ShopService(IEconomyService economy, ISaveManager save, IGameLibrary library, GameEvents events)
    {
        _economy = economy;
        _saveManager = save;
        _library = library;
        _gameEvents = events;
    }

    public void OpenShop(IShopStrategy strategy)
    {
        var run = _saveManager.Data.CurrentRun;

        // --- DETERMINISMO ---
        // Recupera o random provider da Run atual via Bootstrapper
        IRandomProvider random = CardInteractionBootstrapper.IdentityContext.Random;
        
        if (random == null)
        {
            Debug.LogWarning("[ShopService] Contexto de Random não encontrado! Usando Seed temporária de emergência.");
            // Fallback: Seed baseada no tempo (não-determinístico globalmente, mas evita crash)
            random = new SeededRandomProvider(Environment.TickCount);
        }
           
        // Cria uma nova sessão limpa
        string title = strategy.ShopTitle;
        // Agora passamos o random para a estratégia
        List<IPurchasable> stock = strategy.GenerateInventory(run, _library, random);

        CurrentSession = new ShopSession(title, stock);

        Debug.Log($"[ShopService] Sessão iniciada: {title} ({stock.Count} itens)");
        OnStockRefreshed?.Invoke();
    }

    public void TryPurchase(IPurchasable item)
    {
        if (CurrentSession == null)
        {
            Debug.LogError("[ShopService] Tentativa de compra sem sessão ativa.");
            return;
        }

        // 1. Preparação
        var run = _saveManager.Data.CurrentRun;
        var context = new PurchaseContext(run, _gameEvents);

        // 2. Validação
        var failReason = ValidatePurchase(item, context);
        if (failReason != PurchaseFailReason.None)
        {
            OnPurchaseFailed?.Invoke(failReason);
            Debug.Log($"[ShopService] Compra recusada: {failReason}");
            return;
        }

        // 3. Execução (Transação Financeira e Lógica)
        ProcessTransaction(item, context);
    }

    private PurchaseFailReason ValidatePurchase(IPurchasable item, PurchaseContext context)
    {
        // A) Regras do Item (Estado do jogo, Mão cheia, Vida cheia)
        var itemCheck = item.CanPurchase(context);
        if (itemCheck != PurchaseFailReason.None)
        {
            return itemCheck;
        }

        // B) Regras Econômicas (Dinheiro)
        if (_economy.CurrentMoney < item.Price)
        {
            return PurchaseFailReason.InsufficientFunds;
        }

        return PurchaseFailReason.None;
    }

    private void ProcessTransaction(IPurchasable item, PurchaseContext context)
    {
        // A) Debitar Dinheiro
        if (!_economy.TrySpend(item.Price, TransactionType.ShopPurchase))
        {
            OnPurchaseFailed?.Invoke(PurchaseFailReason.InsufficientFunds);
            return;
        }

        // B) Entregar Produto
        item.OnPurchased(context);

        // C) Atualizar Estoque da Sessão
        CurrentSession.TryRemoveItem(item);

        // D) Persistência e Notificação
        _saveManager.SaveGame();

        OnItemPurchased?.Invoke(item);
        OnStockRefreshed?.Invoke();
    }
    public void SellCard(CardInstance cardInstance)
    {
        var run = _saveManager.Data.CurrentRun;
        if (run == null) return;

        if (_library.TryGetCard(cardInstance.TemplateID, out CardData data))
        {
            int sellPrice = data.BaseSellValue;
            if (sellPrice <= 0) sellPrice = 1;

            _economy.Earn(sellPrice, TransactionType.CardSale);

            // Lógica de Remoção Segura
            // Procura a instância exata pelo ID único (GUID)
            var instanceToRemove = run.Hand.Find(x => x.UniqueID == cardInstance.UniqueID);

            if (!string.IsNullOrEmpty(instanceToRemove.UniqueID))
            {
                run.Hand.Remove(instanceToRemove);
                _gameEvents.Player.TriggerCardRemoved(instanceToRemove);
            }

            _saveManager.SaveGame();
            Debug.Log($"[Shop] Vendeu {data.Name} (ID: {cardInstance.UniqueID.Substring(0, 4)}...)");
        }
    }
}
