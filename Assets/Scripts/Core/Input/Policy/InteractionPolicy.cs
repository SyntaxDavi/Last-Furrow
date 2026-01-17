/// <summary>
/// Política de Interação - Decide O QUE pode ser feito baseado no estado do jogo.
/// 
/// Responsabilidade ÚNICA: Responder "posso fazer X agora?"
/// NÃO executa nada, apenas consulta.
/// 
/// Futuramente pode adicionar:
/// - Flags de tutorial
/// - Debuffs temporários
/// - Modo espectador
/// </summary>
public class InteractionPolicy
{
    private readonly GameStateManager _gameState;

    public InteractionPolicy(GameStateManager gameState)
    {
        _gameState = gameState;
    }

    /// <summary>
    /// Hover é permitido em Playing e Shopping.
    /// </summary>
    public bool CanHover()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    /// <summary>
    /// Click é permitido em Playing e Shopping (vender cartas, selecionar grid).
    /// </summary>
    public bool CanClick()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    /// <summary>
    /// Drag só é permitido em Playing (produção).
    /// </summary>
    public bool CanDrag()
    {
        return _gameState.CurrentState == GameState.Playing;
    }

    /// <summary>
    /// Verifica se qualquer input é permitido (para early-out no Update).
    /// </summary>
    public bool IsInputAllowed()
    {
        var state = _gameState.CurrentState;
        return state == GameState.Playing || state == GameState.Shopping;
    }

    /// <summary>
    /// Retorna quais layers devem ser consideradas para hover baseado no estado.
    /// </summary>
    public bool ShouldIncludeDropLayerInHover()
    {
        return _gameState.CurrentState == GameState.Playing;
    }
}
