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