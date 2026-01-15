/// <summary>
/// Estados principais do jogo
/// </summary>
public enum GameState
{
    Initialization,
    MainMenu,
    Playing,
    Shopping,
    Paused,
    GameOver
}

/// <summary>
/// Fases da semana de produção
/// </summary>
public enum RunPhase
{
    Production,
    Weekend
}

/// <summary>
/// Motivos de encerramento da run
/// </summary>
public enum RunEndReason
{
    Victory,            
    HarvestFailed,     
    WitheredOverload,   
    Abandoned           
}
