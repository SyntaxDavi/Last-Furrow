/// <summary>
/// Tipos de transações econômicas
/// </summary>
public enum TransactionType
{
    Debug = 0,
    Harvest,           // Venda de colheita (Principal)
    CardSale,          // Venda de carta
    CardOverflow,      // Queimar carta da mão
    GoalBonus,         // Excedeu meta
    ShopPurchase,      // Gasto na loja
    PestControl,       // Gasto com pragas
    HealthRecovery,    // Gasto com vida
    TraditionCost,     // Custo de tradição
    EventEffect        // Evento aleatório
}
