/// <summary>
/// Interface para acesso read-only ao estado do jogo.
/// Permite desacoplamento de GameStateManager concreto.
/// 
/// RESPONSABILIDADE:
/// - Fornecer acesso somente-leitura ao GameState atual
/// - Permitir mock em testes unitários
/// 
/// BENEFÍCIOS:
/// - Testável: Mock retorna qualquer estado desejado
/// - Desacoplado: Código não depende de GameStateManager concreto
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
    /// Verifica se o gameplay está ativo (Playing).
    /// </summary>
    bool IsGameplayActive();
}
