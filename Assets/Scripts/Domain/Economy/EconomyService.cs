using UnityEngine;

public class EconomyService : IEconomyService
{
    private readonly IRunManager _runManager;
    private readonly ISaveManager _saveManager;

    public event System.Action<int, int, TransactionType> OnBalanceChanged;

    public EconomyService(IRunManager runManager, ISaveManager saveManager)
    {
        _runManager = runManager;
        _saveManager = saveManager;
    }

    public int CurrentMoney
    {
        get
        {
            if (_saveManager.Data?.CurrentRun == null) return 0;
            return _saveManager.Data.CurrentRun.Money;
        }
    }

    public void Earn(int amount, TransactionType source)
    {
        if (amount <= 0) return;
        var run = GetRunSafe();
        if (run == null) return;

        run.Money += amount;
        run.TotalMoneyEarned += amount;

        NotifyAndSave(run.Money, amount, source);
        Debug.Log($"[Economy] Ganhou ${amount} via {source}. Saldo: ${run.Money}");
    }

    public bool TrySpend(int amount, TransactionType reason)
    {
        if (amount <= 0) return false; // Ou true se custo zero for permitido
        var run = GetRunSafe();
        if (run == null) return false;

        if (run.Money >= amount)
        {
            run.Money -= amount;
            NotifyAndSave(run.Money, -amount, reason);
            Debug.Log($"[Economy] Gastou ${amount} em {reason}. Saldo: ${run.Money}");
            return true;
        }

        Debug.Log($"[Economy] Falha ao gastar ${amount} em {reason}. Saldo Insuficiente.");
        return false;
    }

    private void NotifyAndSave(int newBalance, int delta, TransactionType type)
    {
        OnBalanceChanged?.Invoke(newBalance, delta, type);
        _saveManager.SaveGame();
    }

    private RunData GetRunSafe()
    {
        return _saveManager.Data?.CurrentRun;
    }
}