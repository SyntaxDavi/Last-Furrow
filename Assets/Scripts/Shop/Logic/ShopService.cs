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
    // A UI pode ler ShopService.CurrentStock e internamente lemos da Sessão
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

        // Cria uma nova sessão limpa
        string title = strategy.ShopTitle;
        List<IPurchasable> stock = strategy.GenerateInventory(run, _library);

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

        // 2. Validação (Método separado)
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
        // Nota: Verificamos se PODE gastar, mas não gastamos ainda.
        // O EconomyService idealmente teria um "CanSpend", mas usaremos a lógica inversa aqui
        if (_economy.CurrentMoney < item.Price)
        {
            return PurchaseFailReason.InsufficientFunds;
        }

        return PurchaseFailReason.None;
    }

    private void ProcessTransaction(IPurchasable item, PurchaseContext context)
    {
        // A) Debitar Dinheiro
        // Como já validamos, o TrySpend deve passar (a menos que haja race condition, raro aqui)
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
}