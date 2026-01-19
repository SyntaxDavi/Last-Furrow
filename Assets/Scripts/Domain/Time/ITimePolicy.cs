/// <summary>
/// Interface para regras de tempo/calendário do jogo.
/// 
/// RESPONSABILIDADE:
/// - Decidir quando jogador pode dormir
/// - Validar estados temporais
/// - Centralizar lógica de calendário
/// 
/// SOLID:
/// - Single Responsibility: Apenas regras de tempo
/// - Open/Closed: Adicionar novas regras sem modificar código existente
/// - Dependency Inversion: UI pergunta, não decide
/// 
/// FUTURO:
/// - Eventos especiais que alteram calendário
/// - Cartas que permitem dormir extra
/// - Feriados/dias especiais
/// </summary>
public interface ITimePolicy
{
    /// <summary>
    /// Verifica se jogador pode avançar o dia (dormir).
    /// </summary>
    bool CanSleep(int currentDay, RunPhase currentPhase);

    /// <summary>
    /// Verifica se está no fim de semana (Dia 6-7).
    /// </summary>
    bool IsWeekend(int currentDay);

    /// <summary>
    /// Verifica se é o último dia da semana de produção (Dia 5).
    /// </summary>
    bool IsLastProductionDay(int currentDay);
}
