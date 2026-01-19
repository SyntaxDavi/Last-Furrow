/// <summary>
/// Adapter que implementa IRunDataProvider delegando para RunData.
/// 
/// PADRÃO: Adapter Pattern
/// 
/// RESPONSABILIDADE:
/// - Conectar RunData (domínio) com IRunDataProvider (interface UI)
/// - Manter acoplamento unidirecional (UI ? Interface ? Adapter ? RunData)
/// 
/// BENEFÍCIOS:
/// - UI não conhece RunData diretamente
/// - Fácil trocar implementação (ex: mock em testes)
/// - Read-only garantido por interface
/// </summary>
public class RunDataProviderAdapter : IRunDataProvider
{
    private readonly ISaveManager _saveManager;

    public RunDataProviderAdapter(ISaveManager saveManager)
    {
        _saveManager = saveManager ?? throw new System.ArgumentNullException(nameof(saveManager));
    }

    private RunData CurrentRun => _saveManager.Data?.CurrentRun;

    public int CurrentLives => CurrentRun?.CurrentLives ?? 0;
    public int MaxLives => CurrentRun?.MaxLives ?? 3;
    public int CurrentDay => CurrentRun?.CurrentDay ?? 1;
    public int CurrentWeek => CurrentRun?.CurrentWeek ?? 1;
    public int Money => CurrentRun?.Money ?? 0;
    public int CurrentWeeklyScore => CurrentRun?.CurrentWeeklyScore ?? 0;
    public int WeeklyGoalTarget => CurrentRun?.WeeklyGoalTarget ?? 0;
}
