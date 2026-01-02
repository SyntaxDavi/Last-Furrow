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
    Clear       
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