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
    RunEnded,    // Fim de run (vitória ou derrota)
    Analyzing,   // Bloqueia interações de cartas durante análise de grid
    ShowingResult // Bloqueia tudo durante exibição de resultado semanal
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
