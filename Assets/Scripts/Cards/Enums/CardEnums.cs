public enum CardType
{
    Plant,      // Plantação
    Modify,     // Modificador (água, fertilizante)
    Harvest,    // Colheita
    Clear,      // Limpeza
    Care,       // Cuidado
    Expansion   // Desbloqueio de Grid
}

/// <summary>
/// Eventos de crescimento biológico
/// </summary>
public enum GrowthEventType
{
    None,
    Growing,
    Matured,
    WitheredByAge,
    WitheredByOverdose,
    LastFreshDayWarning
}

/// <summary>
/// Eventos de atualização do grid
/// </summary>
public enum GridEventType
{
    GenericUpdate = 0,
    Planted,
    Watered,
    Harvested,
    Matured,
    Withered,
    DryOut,
    ModificationApplied,
    SlotUnlocked,
    ManualRefresh
}
