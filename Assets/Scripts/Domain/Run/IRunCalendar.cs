/// <summary>
/// Abstração para configuração de calendário da Run.
/// Permite que a estrutura temporal (dias de produção, weekend) seja configurável.
/// </summary>
public interface IRunCalendar
{
    /// <summary>
    /// Quantos dias o jogador trabalha antes do weekend (ex: 5).
    /// </summary>
    int ProductionDays { get; }
    
    /// <summary>
    /// Dia em que o weekend começa (ProductionDays + 1).
    /// </summary>
    int WeekendStartDay { get; }
    
    /// <summary>
    /// Total de dias no ciclo semanal (Production + Weekend).
    /// </summary>
    int DaysPerCycle { get; }
    
    /// <summary>
    /// Verifica se o dia informado é um dia de produção.
    /// </summary>
    bool IsProductionDay(int day);
    
    /// <summary>
    /// Verifica se o dia informado é o início do weekend.
    /// </summary>
    bool IsWeekendStart(int day);
    
    /// <summary>
    /// Verifica se o dia informado passou do ciclo (deve resetar para dia 1).
    /// </summary>
    bool IsPastCycle(int day);
    
    /// <summary>
    /// Determina a fase correta para o dia informado.
    /// </summary>
    RunPhase GetPhaseForDay(int day);
}
