using System;

/// <summary>
/// Permite desacoplamento de GameStateManager concreto.
///
/// RESPONSABILIDADE:
/// - Fornecer acesso ao GameState atual
/// - Permitir alteração de estado
/// - Permitir mock em testes unitários
/// </summary>
public interface IGameStateProvider
{
    /// <summary>
    /// Estado atual do jogo.
    /// </summary>
    GameState CurrentState { get; }

    /// <summary>
    /// Estado anterior do jogo (útil para transições).
    /// </summary>
    GameState PreviousState { get; }

    /// <summary>
    /// Evento disparado quando o estado muda.
    /// </summary>
    event Action<GameState> OnStateChanged;

    /// <summary>
    /// Altera o estado do jogo.
    /// </summary>
    void SetState(GameState newState);

    /// <summary>
    /// Verifica se o gameplay está ativo (Playing).
    /// </summary>
    bool IsGameplayActive();
}
