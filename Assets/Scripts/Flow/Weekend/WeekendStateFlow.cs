using UnityEngine;

public class WeekendStateFlow : IWeekendStateFlow
{
    private readonly GameStateManager _gameStateManager;

    // Injeção de Dependência via Construtor
    public WeekendStateFlow(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    public void EnterWeekendState()
    {
        // Muda o estado do jogo para Shopping (bloqueia interações do Grid, muda Input, etc)
        _gameStateManager.SetState(GameState.Shopping);
        Debug.Log("[StateFlow] Entrou no estado: Shopping");
    }

    public void ExitWeekendState()
    {
        // Retorna o jogo para o estado Jogável (Produção)
        _gameStateManager.SetState(GameState.Playing);
        Debug.Log("[StateFlow] Retornou ao estado: Playing");
    }
}