public enum TransactionType
{
    Debug = 0,
    Harvest,           // Venda de colheita (Principal)
    CardSale,          //
    CardOverflow,      // Queimar carta da mão
    GoalBonus,         // Excedeu meta
    ShopPurchase,      // Gasto na loja
    PestControl,       // Gasto com pragas
    HealthRecovery,    // Gasto com vida
    TraditionCost,     // Custo de tradição
    EventEffect        // Evento aleatório
}

public interface IEconomyService
{
    // Leitura
    int CurrentMoney { get; }

    // Ações
    void Earn(int amount, TransactionType source);
    bool TrySpend(int amount, TransactionType reason);

    // Eventos
    // Int: Novo Saldo, Int: Diferença (+/-), Type: Motivo
    event System.Action<int, int, TransactionType> OnBalanceChanged;
}