/// <summary>
/// Interface para acesso read-only aos dados da run.
/// 
/// RESPONSABILIDADE:
/// - Expor apenas dados essenciais para UI
/// - Desacoplar UI do RunData completo
/// - Permitir mocking em testes futuros
/// 
/// SOLID:
/// - Interface Segregation: Apenas o necessário para UI
/// - Dependency Inversion: UI depende de abstração, não implementação
/// </summary>
public interface IRunDataProvider
{
    int CurrentLives { get; }
    int MaxLives { get; }
    int CurrentDay { get; }
    int CurrentWeek { get; }
    int Money { get; }
    int CurrentWeeklyScore { get; }
    int WeeklyGoalTarget { get; }
}
