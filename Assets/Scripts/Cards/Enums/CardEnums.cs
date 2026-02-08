/// <summary>
/// Tipos de cartas disponíveis no jogo
/// </summary>
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
/// Raridade das cartas (afeta frequência no deck)
/// </summary>
public enum CardRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

/// <summary>
/// Tags para categorização e filtros de cartas (flags combinável)
/// </summary>
[System.Flags]
public enum CardTag
{
    None = 0,
    Seed = 1 << 0,
    Water = 1 << 1,
    Tool = 1 << 2,
    Harvest = 1 << 3,
    Seasonal = 1 << 4,
    Premium = 1 << 5
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
