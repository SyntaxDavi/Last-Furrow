public enum GameState
{
    Initialization,
    MainMenu,
    Playing,
    Shopping,
    Paused,
    GameOver
}

public enum RunPhase
{
    Production,
    Weekend
}

public enum RunEndReason
{
    Victory,            
    HarvestFailed,     
    WitheredOverload,   
    Abandoned           
}

public enum CardType
{
    Plant,      
    Modify,     
    Harvest,    
    Clear,
    Care
}

public enum GrowthEventType
{
    None,
    Growing,
    Matured,
    WitheredByAge,
    WitheredByOverdose, 
    LastFreshDayWarning
}

public enum GridEventType
{
    GenericUpdate = 0, 
    Planted,
    Watered,
    Harvested,
    Matured,
    Withered,
    DryOut,
    ModificationApplied
}

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