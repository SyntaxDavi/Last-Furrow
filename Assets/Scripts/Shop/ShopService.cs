using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopService
{
    private readonly IEconomyService _economy;
    private readonly SaveManager _saveManager;
    private readonly IGameLibrary _library;
    private readonly GameEvents _gameEvents; 

    // Estado Volátil (Estoque atual da sessão)
    public List<IPurchasable> CurrentStock { get; private set; }
    public string CurrentShopTitle { get; private set; }

    // Eventos
    public event Action OnStockRefreshed;
    public event Action<IPurchasable> OnItemPurchased;
    public event Action<PurchaseFailReason> OnPurchaseFailed; 

    public ShopService(IEconomyService economy, SaveManager save, IGameLibrary library, GameEvents events)
    {
        _economy = economy;
        _saveManager = save;
        _library = library;
        _gameEvents = events;
        CurrentStock = new List<IPurchasable>();
    }

    public void OpenShop(IShopStrategy strategy)
    {
        var run = _saveManager.Data.CurrentRun;

        CurrentShopTitle = strategy.ShopTitle;
        CurrentStock = strategy.GenerateInventory(run, _library);

        Debug.Log($"[ShopService] Loja Aberta: {CurrentShopTitle} ({CurrentStock.Count} itens)");
        OnStockRefreshed?.Invoke();
    }

    public void TryPurchase(IPurchasable item)
    {
        var run = _saveManager.Data.CurrentRun;

        // 1. Cria o contexto de execução (Injeção de Dependência Local)
        var context = new PurchaseContext(run, _gameEvents);

        // 2. Validação Lógica do Item
        var failReason = item.CanPurchase(context);
        if (failReason != PurchaseFailReason.None)
        {
            OnPurchaseFailed?.Invoke(failReason);
            Debug.Log($"[ShopService] Compra recusada: {failReason}");
            return;
        }

        // 3. Validação Econômica
        if (!_economy.TrySpend(item.Price, TransactionType.ShopPurchase))
        {
            OnPurchaseFailed?.Invoke(PurchaseFailReason.InsufficientFunds);
            return;
        }

        // 4. Sucesso: Executa a compra
        item.OnPurchased(context);

        // 5. Remove do estoque (consumível único por visita)
        CurrentStock.Remove(item);

        // 6. Salva o progresso
        _saveManager.SaveGame();

        OnItemPurchased?.Invoke(item);
        OnStockRefreshed?.Invoke();
    }
}