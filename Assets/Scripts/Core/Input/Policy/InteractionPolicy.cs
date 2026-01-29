/// <summary>
/// Poltica de Interao - Decide O QUE pode ser feito baseado no estado do jogo.
/// 
/// Responsabilidade NICA: Responder "posso fazer X agora?"
/// NO executa nada, apenas consulta.
/// 
/// Futuramente pode adicionar:
/// - Flags de tutorial
/// - Debuffs temporrios
/// - Modo espectador
/// </summary>
public class InteractionPolicy
{
    private readonly GameStateManager _gameState;

    public InteractionPolicy(GameStateManager gameState)
    {
        _gameState = gameState;
    }

    public bool CanHover()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    public bool CanClick()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    public bool CanDrag()
    {
        return _gameState.CurrentState == GameState.Playing;
    }

    public bool IsInputAllowed()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    public bool ShouldIncludeDropLayerInHover()
    {
        return _gameState.CurrentState == GameState.Playing;
    }
}
