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
    GameOver,
    Analyzing  // Bloqueia interações de cartas durante análise de grid
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
