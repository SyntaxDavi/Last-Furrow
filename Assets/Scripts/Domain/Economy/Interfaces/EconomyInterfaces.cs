using System;

/// <summary>
/// Interface para o serviço de economia
/// </summary>
public interface IEconomyService
{
    // Leitura
    int CurrentMoney { get; }

    // Ações
    void Earn(int amount, TransactionType source);
    bool TrySpend(int amount, TransactionType reason);

    // Eventos
    // Int: Novo Saldo, Int: Diferença (+/-), Type: Motivo
    event Action<int, int, TransactionType> OnBalanceChanged;
}
