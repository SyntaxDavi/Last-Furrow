using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Step Final do Pipeline de Exit do Weekend.
/// 
/// SOLID: Single Responsibility - Apenas avança para a próxima semana.
/// 
/// IMPORTANTE: Este step DEVE ser o último do pipeline de exit.
/// Garante que OnProductionStarted só seja disparado APÓS:
/// - Fade Out/In completar
/// - Shop ser limpo
/// - Cards serem distribuídos (WeekendCardDrawStep)
/// 
/// Isso resolve a race condition onde o botão Sleep era
/// reativado antes do draw de cartas completar.
/// </summary>
public class StartNextWeekStep : IFlowStep
{
    private readonly RunManager _runManager;
    private readonly RunData _runData;

    public string Name => "Start Next Week";

    public StartNextWeekStep(RunManager runManager, RunData runData)
    {
        _runManager = runManager;
        _runData = runData;
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_runManager == null || _runData == null)
        {
            Debug.LogError("[StartNextWeekStep] Dependências nulas!");
            return;
        }

        Debug.Log($"[StartNextWeekStep] Avançando para Semana {_runData.CurrentWeek + 1}...");
        
        // Agora StartNextWeek é chamado APÓS todo o pipeline completar
        // Isso garante que OnProductionStarted seja disparado no momento correto
        _runManager.StartNextWeek(_runData);

        // Aguarda 1 frame para garantir que eventos sejam propagados
        await UniTask.Yield();
        
        Debug.Log("[StartNextWeekStep] Semana avançada com sucesso.");
    }
}
